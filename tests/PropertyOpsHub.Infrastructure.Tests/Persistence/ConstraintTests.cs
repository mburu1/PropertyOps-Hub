using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Infrastructure.Tests.Persistence;

/// <summary>
/// Constraints that the database, not the application, has to enforce: two requests can pass
/// an application-level uniqueness check at the same time.
/// </summary>
public class ConstraintTests : IClassFixture<SqlServerFixture>
{
    /// <summary>SQL Server duplicate-key errors.</summary>
    private static readonly int[] DuplicateKeyErrors = [2601, 2627];

    private readonly SqlServerFixture _fixture;

    public ConstraintTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Unique_index_rejects_a_second_live_property_with_the_same_code()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        GroupId groupId;
        await using (var admin = _fixture.CreateContext(TestCurrentUser.System))
        {
            var (group, _, _) = Seed.Organization(admin);
            groupId = group.Id;
        }

        await using var second = _fixture.CreateContext(TestCurrentUser.System);
        second.Properties.Add(Property.Create(groupId, "Nairobi West", "nbo-cbd", "Africa/Nairobi"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());

        Assert.IsType<SqlException>(exception.InnerException);
        var sqlError = (SqlException)exception.InnerException!;
        Assert.Contains(sqlError.Number, DuplicateKeyErrors);
    }

    [Fact]
    public async Task A_soft_deleted_property_frees_its_code()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        PropertyId removedId;
        GroupId groupId;

        await using (var admin = _fixture.CreateContext(TestCurrentUser.System))
        {
            var (group, property, _) = Seed.Organization(admin);
            groupId = group.Id;
            removedId = property.Id;

            // The aggregate's own Delete() is the supported path for soft deletable roots.
            // DbSet.Remove() would throw here: Property has a required Outlet dependent, and
            // EF refuses to sever it before any interceptor runs.
            property.Delete();
            await admin.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext(TestCurrentUser.System);
        var replacement = Property.Create(groupId, "Nairobi West", "nbo-cbd", "Africa/Nairobi");
        context.Properties.Add(replacement);

        await context.SaveChangesAsync();

        Assert.NotEqual(removedId, replacement.Id);
    }

    [Fact]
    public async Task A_property_cannot_reference_a_group_that_does_not_exist()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext(TestCurrentUser.System);
        context.Properties.Add(Property.Create(GroupId.New(), "Orphan", "ORP", "Africa/Nairobi"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.IsType<SqlException>(exception.InnerException);
    }

    [Fact]
    public async Task The_schema_contains_the_expected_objects()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await using var context = _fixture.CreateContext(TestCurrentUser.System);

        var tables = await context.Database
            .SqlQueryRaw<string>(
                "SELECT TABLE_SCHEMA + '.' + TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
            .ToListAsync();

        Assert.Contains("org.Groups", tables);
        Assert.Contains("org.Properties", tables);
        Assert.Contains("org.Outlets", tables);

        var indexes = await context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sys.indexes WHERE name = 'UX_Properties_GroupId_Code'")
            .ToListAsync();

        Assert.Contains("UX_Properties_GroupId_Code", indexes);
        Assert.Single(await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.indexes WHERE name = 'UX_Properties_GroupId_Code' AND is_unique = 1")
            .ToListAsync());
    }
}
