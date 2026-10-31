using System;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Domain.Tests.Organization;

public class OutletTests
{
    private static readonly PropertyId AnyProperty = PropertyId.New();

    [Fact]
    public void Create_assigns_identity_and_raises_an_event()
    {
        var outlet = Outlet.Create(AnyProperty, "  Pool Bar ", OutletType.Bar);

        Assert.False(outlet.Id.IsEmpty);
        Assert.Equal(AnyProperty, outlet.PropertyId);
        Assert.Equal("Pool Bar", outlet.Name);
        Assert.Equal(OutletType.Bar, outlet.Type);
        Assert.True(outlet.IsActive);

        var created = Assert.IsType<OutletCreated>(Assert.Single(outlet.DomainEvents));
        Assert.Equal(outlet.Id, created.OutletId);
        Assert.Equal(OutletType.Bar, created.Type);
    }

    [Fact]
    public void Create_rejects_an_empty_property_id()
    {
        var exception = Assert.Throws<DomainException>(
            () => Outlet.Create(default, "Pool Bar", OutletType.Bar));

        Assert.Equal("domain.value.required", exception.Code);
    }

    [Fact]
    public void Create_rejects_an_undefined_outlet_type()
    {
        var exception = Assert.Throws<DomainException>(
            () => Outlet.Create(AnyProperty, "Pool Bar", (OutletType)99));

        Assert.Equal("domain.value.out_of_range", exception.Code);
    }

    [Fact]
    public void Rename_and_ChangeType_validate_their_input()
    {
        var outlet = Outlet.Create(AnyProperty, "Pool Bar", OutletType.Bar);

        outlet.Rename("  Terrace Bar ");
        Assert.Equal("Terrace Bar", outlet.Name);
        Assert.Throws<DomainException>(() => outlet.Rename(" "));

        outlet.ChangeType(OutletType.Restaurant);
        Assert.Equal(OutletType.Restaurant, outlet.Type);
        Assert.Throws<DomainException>(() => outlet.ChangeType((OutletType)0));
    }

    [Fact]
    public void Delete_marks_the_outlet_as_soft_deleted()
    {
        var outlet = Outlet.Create(AnyProperty, "Spa", OutletType.Spa);

        outlet.Delete();

        Assert.True(outlet.IsDeleted);
    }
}
