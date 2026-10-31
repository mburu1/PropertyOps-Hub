using System;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.MaterialControl;

namespace PropertyOpsHub.Domain.Tests.MaterialControl;

public class ItemTests
{
    private static Item NewItem() =>
        Item.Create("beef-topping", "Beef Topping", ItemCategory.Food, UnitOfMeasure.Kilogram);

    [Fact]
    public void Create_normalises_the_code_and_sets_defaults()
    {
        var item = NewItem();

        Assert.Equal("BEEF-TOPPING", item.Code);
        Assert.Equal("Beef Topping", item.Name);
        Assert.Equal(ItemCategory.Food, item.Category);
        Assert.Equal(UnitOfMeasure.Kilogram, item.BaseUnit);
        Assert.Equal(CostingMethod.WeightedAverage, item.CostingMethod);
        Assert.True(item.IsActive);
        Assert.False(item.Id.IsEmpty);
        Assert.Empty(item.Conversions);

        var created = Assert.IsType<ItemCreated>(Assert.Single(item.DomainEvents));
        Assert.Equal(item.Id, created.ItemId);
        Assert.Equal("BEEF-TOPPING", created.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("HAS SPACE")]
    [InlineData("YOGHURT/PLAIN")]
    public void Create_rejects_an_invalid_code(string code)
    {
        var exception = Assert.Throws<DomainException>(
            () => Item.Create(code, "Beef Topping", ItemCategory.Food, UnitOfMeasure.Kilogram));

        Assert.Equal("material.item.code_invalid", exception.Code);
    }

    [Fact]
    public void Create_rejects_an_empty_name()
    {
        var exception = Assert.Throws<DomainException>(
            () => Item.Create("BEEF", "  ", ItemCategory.Food, UnitOfMeasure.Kilogram));

        Assert.Equal("domain.value.required", exception.Code);
    }

    [Fact]
    public void Create_rejects_an_undefined_category()
    {
        var exception = Assert.Throws<DomainException>(
            () => Item.Create("BEEF", "Beef", (ItemCategory)42, UnitOfMeasure.Kilogram));

        Assert.Equal("domain.value.out_of_range", exception.Code);
    }

    [Fact]
    public void A_conversion_turns_purchase_units_into_base_units()
    {
        var item = NewItem();

        item.AddConversion(UnitOfMeasure.Case, 12m);

        Assert.Equal(12m, item.FactorFor(UnitOfMeasure.Case));
        Assert.Equal(24m, item.ToBaseUnits(2m, UnitOfMeasure.Case));
        Assert.Equal(
            Quantity.Of(24m, UnitOfMeasure.Kilogram),
            item.ToBase(Quantity.Of(2m, UnitOfMeasure.Case)));
    }

    [Fact]
    public void The_base_unit_needs_no_conversion()
    {
        var item = NewItem();

        Assert.Equal(1m, item.FactorFor(UnitOfMeasure.Kilogram));
        Assert.Equal(3m, item.ToBaseUnits(3m, UnitOfMeasure.Kilogram));
    }

    [Fact]
    public void Converting_a_unit_twice_is_refused()
    {
        var item = NewItem();
        item.AddConversion(UnitOfMeasure.Case, 12m);

        var exception = Assert.Throws<DomainException>(
            () => item.AddConversion(UnitOfMeasure.Case, 6m));

        Assert.Equal("material.item.conversion_duplicate", exception.Code);
    }

    [Fact]
    public void The_base_unit_cannot_have_a_conversion()
    {
        var exception = Assert.Throws<DomainException>(
            () => NewItem().AddConversion(UnitOfMeasure.Kilogram, 1m));

        Assert.Equal("material.item.conversion_to_base", exception.Code);
    }

    [Fact]
    public void An_unknown_unit_cannot_be_converted()
    {
        var exception = Assert.Throws<DomainException>(
            () => NewItem().FactorFor(UnitOfMeasure.Case));

        Assert.Equal("material.item.conversion_missing", exception.Code);
    }

    [Fact]
    public void Removing_a_conversion_that_does_not_exist_is_refused()
    {
        var exception = Assert.Throws<DomainException>(
            () => NewItem().RemoveConversion(UnitOfMeasure.Case));

        Assert.Equal("material.item.conversion_missing", exception.Code);
    }

    [Fact]
    public void RemoveConversion_drops_the_factor()
    {
        var item = NewItem();
        item.AddConversion(UnitOfMeasure.Case, 12m);

        item.RemoveConversion(UnitOfMeasure.Case);

        Assert.Empty(item.Conversions);
        Assert.Throws<DomainException>(() => item.FactorFor(UnitOfMeasure.Case));
    }

    [Fact]
    public void Rename_and_recategorise_validate_their_input()
    {
        var item = NewItem();

        item.Rename("  Beef Topside ");
        item.Recategorize(ItemCategory.Beverage);

        Assert.Equal("Beef Topside", item.Name);
        Assert.Equal(ItemCategory.Beverage, item.Category);
        Assert.Throws<DomainException>(() => item.Rename(" "));
        Assert.Throws<DomainException>(() => item.Recategorize((ItemCategory)0));
    }

    [Fact]
    public void Deactivate_and_delete_flags_are_independent()
    {
        var item = NewItem();

        item.Deactivate();
        Assert.False(item.IsActive);

        item.Activate();
        item.Delete();

        Assert.True(item.IsActive);
        Assert.True(item.IsDeleted);
    }
}
