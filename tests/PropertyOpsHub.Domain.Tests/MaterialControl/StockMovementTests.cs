using System;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.MaterialControl;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Domain.Tests.MaterialControl;

public class StockMovementTests
{
    private static readonly PropertyId AnyProperty = PropertyId.New();
    private static readonly ItemId AnyItem = ItemId.New();
    private static readonly DateOnly Today = new(2026, 10, 31);

    private static StockMovement Record(
        StockMovementType type = StockMovementType.GoodsReceipt,
        decimal quantity = 10m,
        decimal unitCost = 25m,
        string reference = "GRN-0001") =>
        StockMovement.Record(
            AnyProperty,
            AnyItem,
            type,
            quantity,
            Money.Of(unitCost, "KES"),
            Today,
            reference);

    [Fact]
    public void Record_captures_the_entry()
    {
        var movement = Record();

        Assert.False(movement.Id.IsEmpty);
        Assert.Equal(AnyProperty, movement.PropertyId);
        Assert.Equal(AnyItem, movement.ItemId);
        Assert.Equal(StockMovementType.GoodsReceipt, movement.Type);
        Assert.Equal(10m, movement.Quantity);
        Assert.Equal(Money.Of(25m, "KES"), movement.UnitCost);
        Assert.Equal(Today, movement.BusinessDate);
        Assert.Equal("GRN-0001", movement.Reference);
        Assert.Null(movement.ModifiedAtUtc);
    }

    [Theory]
    [InlineData(StockMovementType.GoodsReceipt)]
    [InlineData(StockMovementType.AdjustmentIn)]
    [InlineData(StockMovementType.TransferIn)]
    public void Inbound_types_add_to_the_balance(StockMovementType type) =>
        Assert.Equal(10m, Record(type).SignedQuantity);

    [Theory]
    [InlineData(StockMovementType.Issue)]
    [InlineData(StockMovementType.Wastage)]
    [InlineData(StockMovementType.AdjustmentOut)]
    [InlineData(StockMovementType.TransferOut)]
    public void Outbound_types_subtract_from_the_balance(StockMovementType type) =>
        Assert.Equal(-10m, Record(type).SignedQuantity);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Record_rejects_a_quantity_that_is_not_positive(decimal quantity)
    {
        var exception = Assert.Throws<DomainException>(() => Record(quantity: quantity));

        Assert.Equal("domain.value.out_of_range", exception.Code);
    }

    [Fact]
    public void Record_rejects_a_negative_unit_cost()
    {
        var exception = Assert.Throws<DomainException>(() => Record(unitCost: -1m));

        Assert.Equal("material.stock.cost_negative", exception.Code);
    }

    [Fact]
    public void Record_rejects_a_missing_business_date()
    {
        var exception = Assert.Throws<DomainException>(
            () => StockMovement.Record(
                AnyProperty,
                AnyItem,
                StockMovementType.GoodsReceipt,
                1m,
                Money.Of(1m, "KES"),
                default,
                "GRN-0002"));

        Assert.Equal("material.stock.business_date_required", exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_rejects_a_blank_reference(string reference)
    {
        var exception = Assert.Throws<DomainException>(() => Record(reference: reference));

        Assert.Equal("domain.value.required", exception.Code);
    }

    [Fact]
    public void Record_rejects_empty_ids()
    {
        var exception = Assert.Throws<DomainException>(
            () => StockMovement.Record(
                default,
                AnyItem,
                StockMovementType.GoodsReceipt,
                1m,
                Money.Of(1m, "KES"),
                Today,
                "GRN-0003"));

        Assert.Equal("domain.value.required", exception.Code);
    }

    [Fact]
    public void Record_rejects_an_undefined_type()
    {
        var exception = Assert.Throws<DomainException>(
            () => Record(type: (StockMovementType)99));

        Assert.Equal("domain.value.out_of_range", exception.Code);
    }
}
