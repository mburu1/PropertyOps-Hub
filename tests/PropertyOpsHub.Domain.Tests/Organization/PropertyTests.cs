using System;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Domain.Tests.Organization;

public class PropertyTests
{
    private static readonly GroupId AnyGroup = GroupId.New();

    [Fact]
    public void Create_assigns_identity_tenancy_and_code()
    {
        var now = new DateTimeOffset(2026, 10, 31, 8, 0, 0, TimeSpan.Zero);

        var property = Property.Create(AnyGroup, " Nairobi CBD ", "nbo-cbd", "Africa/Nairobi", new TestTimeProvider(now));

        Assert.False(property.Id.IsEmpty);
        Assert.Equal(AnyGroup, property.GroupId);
        Assert.Equal("Nairobi CBD", property.Name);
        Assert.Equal("NBO-CBD", property.Code.Value);
        Assert.Equal("Africa/Nairobi", property.TimeZoneId);
        Assert.True(property.IsActive);
        Assert.Equal(now, property.CreatedAtUtc);

        var created = Assert.IsType<PropertyCreated>(Assert.Single(property.DomainEvents));
        Assert.Equal(property.Id, created.PropertyId);
        Assert.Equal(AnyGroup, created.GroupId);
        Assert.Equal("NBO-CBD", created.Code);
    }

    [Fact]
    public void Create_defaults_the_time_zone_to_utc()
    {
        var property = Property.Create(AnyGroup, "Kisumu", "KSM");

        Assert.Equal("UTC", property.TimeZoneId);
    }

    [Fact]
    public void Create_rejects_an_empty_group_id()
    {
        var exception = Assert.Throws<DomainException>(
            () => Property.Create(default, "Nairobi", "NBO"));

        Assert.Equal("domain.value.required", exception.Code);
    }

    [Fact]
    public void Create_rejects_an_invalid_code()
    {
        var exception = Assert.Throws<DomainException>(
            () => Property.Create(AnyGroup, "Nairobi", "NBO CBD"));

        Assert.Equal("organization.property.code_invalid", exception.Code);
    }

    [Fact]
    public void Create_rejects_a_blank_time_zone()
    {
        var exception = Assert.Throws<DomainException>(
            () => Property.Create(AnyGroup, "Nairobi", "NBO", " "));

        Assert.Equal("domain.value.required", exception.Code);
    }

    [Fact]
    public void Rename_trims_the_value()
    {
        var property = Property.Create(AnyGroup, "Nairobi", "NBO");

        property.Rename("  Nairobi Westlands ");

        Assert.Equal("Nairobi Westlands", property.Name);
    }

    [Fact]
    public void ChangeCode_stores_a_normalised_code()
    {
        var property = Property.Create(AnyGroup, "Nairobi", "NBO");

        property.ChangeCode(PropertyCode.Create("nbo-w"));

        Assert.Equal("NBO-W", property.Code.Value);
    }

    [Fact]
    public void ChangeCode_rejects_null()
    {
        var property = Property.Create(AnyGroup, "Nairobi", "NBO");

        Assert.Throws<ArgumentNullException>(() => property.ChangeCode(null!));
    }

    [Fact]
    public void ChangeTimeZone_validates_the_value()
    {
        var property = Property.Create(AnyGroup, "Nairobi", "NBO");

        property.ChangeTimeZone("Europe/London");

        Assert.Equal("Europe/London", property.TimeZoneId);
        Assert.Throws<DomainException>(() => property.ChangeTimeZone(""));
    }

    [Fact]
    public void Deactivate_then_activate_round_trips()
    {
        var property = Property.Create(AnyGroup, "Nairobi", "NBO");

        property.Deactivate();
        Assert.False(property.IsActive);

        property.Activate();
        Assert.True(property.IsActive);
    }
}
