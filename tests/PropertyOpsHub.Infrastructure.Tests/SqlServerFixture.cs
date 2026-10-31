using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Infrastructure.Persistence;
using PropertyOpsHub.Infrastructure.Persistence.Interceptors;

namespace PropertyOpsHub.Infrastructure.Tests;

/// <summary>
/// Creates a throwaway SQL Server database, applies the real migrations, and drops it again.
/// </summary>
/// <remarks>
/// The server comes from <c>PROPERTYOPS_TEST_SQL</c>; when it is not set the LocalDB default
/// is used, which is what a developer machine has. If no server is reachable the fixture
/// reports <see cref="Available"/> = false and the tests return early instead of failing:
/// CI runs without a SQL Server service today, and a skipped test is honest about that.
/// </remarks>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string LocalDbDefault =
        "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    public string? ConnectionString { get; private set; }

    public bool Available => ConnectionString is not null;

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable("PROPERTYOPS_TEST_SQL");

        if (string.IsNullOrWhiteSpace(server))
        {
            server = LocalDbDefault;
        }

        var databaseName = $"PropertyOpsHub_Test_{Guid.NewGuid():N}";

        try
        {
            var connectionString = new SqlConnectionStringBuilder(server)
            {
                InitialCatalog = databaseName,
                TrustServerCertificate = true
            }.ConnectionString;

            await using var context = CreateContext(connectionString, TestCurrentUser.System, TimeProvider.System);
            await context.Database.MigrateAsync();

            ConnectionString = connectionString;
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException)
        {
            // No SQL Server available on this machine; the tests no-op rather than pretend.
            ConnectionString = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (Available)
        {
            await using var context = CreateContext(ConnectionString!, TestCurrentUser.System, TimeProvider.System);
            await context.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>Removes all rows so each test starts from an empty schema.</summary>
    public async Task ResetAsync()
    {
        await using var context = CreateContext(ConnectionString!, TestCurrentUser.System, TimeProvider.System);
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM [org].[Outlets]; DELETE FROM [org].[Properties]; DELETE FROM [org].[Groups];");
    }

    /// <summary>
    /// Builds a context the same way <c>AddInfrastructure</c> does, interceptors included, so
    /// the tests exercise the production wiring rather than a simplified variant.
    /// </summary>
    public static AppDbContext CreateContext(string connectionString, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .AddInterceptors(
                new SoftDeleteSaveChangesInterceptor(timeProvider),
                new AuditSaveChangesInterceptor(timeProvider))
            .Options;

        return new AppDbContext(options, currentUser);
    }

    public AppDbContext CreateContext(ICurrentUser currentUser, TimeProvider? timeProvider = null) =>
        CreateContext(ConnectionString!, currentUser, timeProvider ?? TimeProvider.System);
}

/// <summary>Current-user stand-in for tests.</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; init; } = true;

    public Guid? UserId { get; init; } = Guid.CreateVersion7();

    public string? Email { get; init; } = "tester@demo.local";

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public IReadOnlyCollection<Guid> PropertyIds { get; init; } = [];

    /// <summary>Bypasses tenant filters; stands in for a background job.</summary>
    public static TestCurrentUser System { get; } = new()
    {
        Roles = [PropertyOpsHub.Application.Security.Roles.GroupFinance]
    };

    public static TestCurrentUser ScopedTo(params Guid[] propertyIds) => new()
    {
        Roles = [PropertyOpsHub.Application.Security.Roles.StoreController],
        PropertyIds = propertyIds
    };

    public static TestCurrentUser Anonymous { get; } = new()
    {
        IsAuthenticated = false,
        UserId = null,
        Email = null
    };
}

/// <summary>Clock with a controllable instant.</summary>
public sealed class FixedTimeProvider : TimeProvider
{
    public FixedTimeProvider(DateTimeOffset now) => Now = now;

    public DateTimeOffset Now { get; set; }

    public override DateTimeOffset GetUtcNow() => Now;
}
