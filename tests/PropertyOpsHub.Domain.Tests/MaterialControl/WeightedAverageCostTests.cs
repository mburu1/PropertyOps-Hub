using System;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.MaterialControl;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Domain.Tests.MaterialControl;

public class WeightedAverageCostTests
{
    private static readonly PropertyId AnyProperty = PropertyId.New();
    private static readonly ItemId AnyItem = ItemId.New();

    private static StockMovement Movement(
        StockMovementType type,
        decimal quantity,
        decimal unitCost,
        int day) =>
        StockMovement.Record(
            AnyProperty,
            AnyItem,
            type,
            quantity,
            Money.Of(unitCost, "USD"),
            new DateOnly(2026, 10, day),
            $"REF-{day}-{type}");

    [Fact]
    public void A_first_receipt_sets_the_average()
    {
        var average = WeightedAverageCost.Blend(null, 0m, Money.Of(10m, "USD"), 5m);

        Assert.Equal(Money.Of(10m, "USD"), average);
    }

    [Fact]
    public void Blending_two_receipts_averages_by_value()
    {
        var average = WeightedAverageCost.Blend(Money.Of(10m, "USD"), 10m, Money.Of(20m, "USD"), 10m);

        Assert.Equal(15m, average.Amount);
        Assert.Equal("USD", average.Currency);
    }

    [Fact]
    public void Blending_uses_quantities_not_a_straight_average()
    {
        // (10 * 10 + 5 * 20) / 15 = 13.3333
        var average = WeightedAverageCost.Blend(Money.Of(10m, "USD"), 10m, Money.Of(20m, "USD"), 5m);

        Assert.Equal(13.3333m, average.Amount);
    }

    [Fact]
    public void A_negative_position_restarts_from_the_receipt_cost()
    {
        // Oversold stock has no meaningful cost basis to blend with.
        var average = WeightedAverageCost.Blend(Money.Of(99m, "USD"), -4m, Money.Of(10m, "USD"), 6m);

        Assert.Equal(10m, average.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_receipt_must_have_a_positive_quantity(decimal quantity)
    {
        var exception = Assert.Throws<DomainException>(
            () => WeightedAverageCost.Blend(null, 0m, Money.Of(1m, "USD"), quantity));

        Assert.Equal("domain.value.out_of_range", exception.Code);
    }

    [Fact]
    public void Mixing_currencies_is_refused()
    {
        var exception = Assert.Throws<DomainException>(
            () => WeightedAverageCost.Blend(Money.Of(10m, "USD"), 5m, Money.Of(10m, "KES"), 5m));

        Assert.Equal("finance.money.currency_mismatch", exception.Code);
    }

    [Fact]
    public void An_empty_ledger_has_no_cost_basis()
    {
        var position = WeightedAverageCost.Calculate([]);

        Assert.Equal(0m, position.Quantity);
        Assert.Null(position.AverageUnitCost);
        Assert.Null(position.Value);
        Assert.True(position.IsEmpty);
    }

    [Fact]
    public void Receipts_and_issues_produce_a_position_and_a_value()
    {
        var position = WeightedAverageCost.Calculate(
        [
            Movement(StockMovementType.GoodsReceipt, 10m, 10m, 1),
            Movement(StockMovementType.GoodsReceipt, 10m, 20m, 2),
            Movement(StockMovementType.Issue, 5m, 15m, 3)
        ]);

        Assert.Equal(15m, position.Quantity);
        Assert.Equal(15m, position.AverageUnitCost!.Amount);
        Assert.Equal(225m, position.Value!.Amount);
    }

    [Fact]
    public void Issues_do_not_change_the_average()
    {
        var position = WeightedAverageCost.Calculate(
        [
            Movement(StockMovementType.GoodsReceipt, 10m, 10m, 1),
            Movement(StockMovementType.Issue, 10m, 999m, 2)
        ]);

        Assert.Equal(0m, position.Quantity);
        Assert.Equal(10m, position.AverageUnitCost!.Amount);
    }

    [Fact]
    public void Oversold_stock_stays_visible_as_a_negative_position()
    {
        var position = WeightedAverageCost.Calculate(
        [
            Movement(StockMovementType.GoodsReceipt, 5m, 10m, 1),
            Movement(StockMovementType.Issue, 10m, 10m, 2)
        ]);

        Assert.Equal(-5m, position.Quantity);
        Assert.True(position.IsNegative);

        // The negative value is the point: silently clamping to zero would hide the error.
        Assert.Equal(-50m, position.Value!.Amount);
    }

    [Fact]
    public void Movements_are_replayed_in_business_date_order()
    {
        // Entered out of order on purpose: the 20-cost receipt belongs before the 10-cost one.
        var position = WeightedAverageCost.Calculate(
        [
            Movement(StockMovementType.GoodsReceipt, 10m, 10m, 2),
            Movement(StockMovementType.GoodsReceipt, 10m, 20m, 1)
        ]);

        Assert.Equal(20m, position.Quantity);
        Assert.Equal(15m, position.AverageUnitCost!.Amount);
    }

    [Fact]
    public void Wastage_reduces_the_balance_without_touching_the_average()
    {
        var position = WeightedAverageCost.Calculate(
        [
            Movement(StockMovementType.GoodsReceipt, 20m, 8m, 1),
            Movement(StockMovementType.Wastage, 2m, 8m, 2)
        ]);

        Assert.Equal(18m, position.Quantity);
        Assert.Equal(8m, position.AverageUnitCost!.Amount);
    }
}
