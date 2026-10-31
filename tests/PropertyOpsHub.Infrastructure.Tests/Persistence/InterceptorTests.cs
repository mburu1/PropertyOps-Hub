using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Infrastructure.Tests.Persistence;

/// <summary>
/// Proves the save interceptors: timestamps come from the injected clock, creation metadata is
/// write-once, and a delete never reaches the database as a DELETE.
/// </summary>
public class InterceptorTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public InterceptorTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Creation_timestamp_comes_from_the_clock_not_the_aggregate()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        // The aggregate stamps DateTimeOffset.UtcNow; the interceptor must overwrite it with
        // the injected clock, otherwise tests and replays are not deterministic.
        var clock = new FixedTimeProvider(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await using var context = _fixture.CreateContext(TestCurrentUser.System, clock);
        var group = Group.Create("Clock Test", "KES");
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        Assert.Equal(clock.Now, group.CreatedAtUtc);
        Assert.Null(group.ModifiedAtUtc);
    }

    [Fact]
    public async Task Update_stamps_ModifiedAtUtc_but_leaves_CreatedAtUtc_alone()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        var clock = new FixedTimeProvider(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        GroupId groupId;
        DateTimeOffset createdAt;

        await using (var context = _fixture.CreateContext(TestCurrentUser.System, clock))
        {
            var group = Group.Create("Clock Test", "KES");
            context.Groups.Add(group);
            await context.SaveChangesAsync();

            groupId = group.Id;
            createdAt = group.CreatedAtUtc;
        }

        clock.Now = new DateTimeOffset(2021, 6, 15, 12, 30, 0, TimeSpan.Zero);

        await using (var context = _fixture.CreateContext(TestCurrentUser.System, clock))
        {
            var group = await context.Groups.SingleAsync(candidate => candidate.Id == groupId);

            // A crafted payload must not be able to rewrite creation metadata.
            context.Entry(group).Property("CreatedAtUtc").CurrentValue =
                new DateTimeOffset(1999, 1, 1, 0, 0, 0, TimeSpan.Zero);

            group.Rename("Renamed Group");
            await context.SaveChangesAsync();
        }

        await using var verification = _fixture.CreateContext(TestCurrentUser.System, clock);
        var stored = await verification.Groups.SingleAsync(candidate => candidate.Id == groupId);

        Assert.Equal("Renamed Group", stored.Name);
        Assert.Equal(createdAt, stored.CreatedAtUtc);
        Assert.Equal(clock.Now, stored.ModifiedAtUtc);
    }

    [Fact]
    public async Task Remove_is_turned_into_a_soft_delete()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        var clock = new FixedTimeProvider(new DateTimeOffset(2022, 3, 4, 5, 6, 7, TimeSpan.Zero));
        OutletId outletId;

        await using (var context = _fixture.CreateContext(TestCurrentUser.System, clock))
        {
            var (_, _, outlet) = Seed.Organization(context);
            outletId = outlet.Id;

            context.Outlets.Remove(outlet);
            await context.SaveChangesAsync();
        }

        await using var verification = _fixture.CreateContext(TestCurrentUser.System, clock);

        // The row is still there for the audit trail...
        var raw = await verification.Outlets
            .IgnoreQueryFilters()
            .SingleAsync(outlet => outlet.Id == outletId);

        Assert.True(raw.IsDeleted);
        Assert.Equal(clock.Now, raw.ModifiedAtUtc);

        // ...and invisible through the normal query path.
        var visible = await verification.Outlets.SingleOrDefaultAsync(outlet => outlet.Id == outletId);
        Assert.Null(visible);
    }
}
