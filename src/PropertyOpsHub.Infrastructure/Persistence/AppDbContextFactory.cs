using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropertyOpsHub.Infrastructure.Persistence;

/// <summary>
/// Builds a context for <c>dotnet ef</c> without starting the API host.
/// </summary>
/// <remarks>
/// The connection string comes from <c>PROPERTYOPS_CONNECTION</c>; the LocalDB default is a
/// developer convenience so <c>dotnet ef migrations add</c> works on a fresh clone. Migrations
/// only read the model, so no database has to be reachable.
/// </remarks>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string LocalDbFallback =
        "Server=(localdb)\\MSSQLLocalDB;Database=PropertyOpsHub;Trusted_Connection=True;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PROPERTYOPS_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = LocalDbFallback;
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options, NullCurrentUser.Instance);
    }
}
