using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Infrastructure.Tests.Persistence;

/// <summary>
/// Proves the global query filters, which are the tenancy boundary (ADR 0002). If one of
/// these fail, one property can read another property's data — so they are the most
/// important tests in this project.
/// </summary>
public class QueryFilterTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public QueryFilterTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task An_anonymous_context_sees_no_rows_at_all()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        await using (var admin = _fixture.CreateContext(TestCurrentUser.System))
        {
            Seed.Organization(admin);
        }

        await using var anonymous = _fixture.CreateContext(TestCurrentUser.Anonymous);

        Assert.Empty(await anonymous.Groups.ToListAsync());
        Assert.Empty(await anonymous.Properties.ToListAsync());
        Assert.Empty(await anonymous.Outlets.ToListAsync());
    }

    [Fact]
    public async Task A_property_scoped_user_sees_only_their_own_property_and_outlets()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        Guid ownPropertyId;
        Guid otherPropertyId;

        await using (var admin = _fixture.CreateContext(TestCurrentUser.System))
        {
            var (group, property, _) = Seed.Organization(admin);

            var other = Property.Create(group.Id, "Mombasa Beach", "MSA-BEACH", "Africa/Nairobi");
            admin.Properties.Add(other);
            await admin.SaveChangesAsync();

            ownPropertyId = property.Id.Value;
            otherPropertyId = other.Id.Value;
        }

        await using var scoped = _fixture.CreateContext(TestCurrentUser.ScopedTo(ownPropertyId));

        var properties = await scoped.Properties.ToListAsync();
        var outlets = await scoped.Outlets.ToListAsync();

        Assert.Single(properties);
        Assert.Equal(ownPropertyId, properties[0].Id.Value);

        Assert.Single(outlets);
        Assert.Equal(ownPropertyId, outlets[0].PropertyId.Value);

        Assert.DoesNotContain(properties, candidate => candidate.Id.Value == otherPropertyId);
    }

    [Fact]
    public async Task A_group_wide_role_sees_every_property()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        await using (var admin = _fixture.CreateContext(TestCurrentUser.System))
        {
            var (group, _, _) = Seed.Organization(admin);
            admin.Properties.Add(Property.Create(group.Id, "Mombasa Beach", "MSA-BEACH", "Africa/Nairobi"));
            await admin.SaveChangesAsync();
        }

        await using var finance = _fixture.CreateContext(TestCurrentUser.System);

        Assert.Equal(2, await finance.Properties.CountAsync());
        Assert.Single(await finance.Groups.ToListAsync());
    }

    [Fact]
    public async Task Groups_are_hidden_from_property_scoped_users()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        Guid propertyId;

        await using (var admin = _fixture.CreateContext(TestCurrentUser.System))
        {
            var (_, property, _) = Seed.Organization(admin);
            propertyId = property.Id.Value;
        }

        await using var scoped = _fixture.CreateContext(TestCurrentUser.ScopedTo(propertyId));

        // A store controller works inside a property and has no business reading tenant roots.
        Assert.Empty(await scoped.Groups.ToListAsync());
        Assert.Single(await scoped.Properties.ToListAsync());
    }

    [Fact]
    public async Task A_user_with_no_property_claims_sees_nothing()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        await using (var admin = _fixture.CreateContext(TestCurrentUser.System))
        {
            Seed.Organization(admin);
        }

        // Authenticated but with no scope: fail closed, not open.
        await using var unclaimed = _fixture.CreateContext(TestCurrentUser.ScopedTo());

        Assert.Empty(await unclaimed.Properties.ToListAsync());
        Assert.Empty(await unclaimed.Outlets.ToListAsync());
    }

    [Fact]
    public async Task IgnoreQueryFilters_is_the_explicit_escape_hatch_for_maintenance()
    {
        if (!_fixture.Available)
        {
            return;
        }

        await _fixture.ResetAsync();

        await using (var admin = _fixture.CreateContext(TestCurrentUser.System))
        {
            Seed.Organization(admin);
        }

        await using var anonymous = _fixture.CreateContext(TestCurrentUser.Anonymous);

        var bypassed = await anonymous.Outlets.IgnoreQueryFilters().ToListAsync();

        Assert.Single(bypassed);
    }
}
