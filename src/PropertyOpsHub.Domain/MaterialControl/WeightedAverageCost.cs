using System;
using System.Collections.Generic;
using System.Linq;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>
/// Weighted-average costing. Because the ledger is append only, the average is not stored on a
/// row; it is recalculated by replaying a position's movements in order.
/// </summary>
public static class WeightedAverageCost
{
    /// <summary>Unit costs are kept to four decimals; anything finer is noise in a kitchen.</summary>
    public const int UnitCostDecimals = 4;

    /// <summary>
    /// Blends a receipt into the running average.
    /// </summary>
    /// <param name="currentAverage">Average before the receipt, or <c>null</c> for a new position.</param>
    /// <param name="currentQuantity">Quantity on hand before the receipt.</param>
    /// <param name="receiptUnitCost">Cost per base unit of the incoming stock.</param>
    /// <param name="receiptQuantity">Quantity received, in base units. Must be positive.</param>
    /// <returns>
    /// The receipt cost when the position is empty or negative: there is nothing to average
    /// against, and inventing a prior cost would distort the valuation.
    /// </returns>
    public static Money Blend(
        Money? currentAverage,
        decimal currentQuantity,
        Money receiptUnitCost,
        decimal receiptQuantity)
    {
        ArgumentNullException.ThrowIfNull(receiptUnitCost);
        _ = Guard.Positive(receiptQuantity, nameof(receiptQuantity));

        if (currentAverage is null || currentQuantity <= 0m)
        {
            return receiptUnitCost.Round(UnitCostDecimals);
        }

        var currentValue = currentAverage.Multiply(currentQuantity);
        var receiptValue = receiptUnitCost.Multiply(receiptQuantity);
        var totalQuantity = currentQuantity + receiptQuantity;

        // Add() enforces that both sides are in the same currency.
        return currentValue
            .Add(receiptValue)
            .Multiply(1m / totalQuantity)
            .Round(UnitCostDecimals);
    }

    /// <summary>
    /// Replays movements in business order and returns the resulting position.
    /// </summary>
    /// <remarks>
    /// Ordering is business date first, then record time, then id, so a replay is deterministic
    /// even when several entries share a date.
    /// </remarks>
    public static StockPosition Calculate(IEnumerable<StockMovement> movements)
    {
        ArgumentNullException.ThrowIfNull(movements);

        var ordered = movements
            .OrderBy(movement => movement.BusinessDate)
            .ThenBy(movement => movement.CreatedAtUtc)
            .ThenBy(movement => movement.Id.Value);

        decimal quantity = 0m;
        Money? average = null;

        foreach (var movement in ordered)
        {
            if (movement.Type.IsInbound())
            {
                average = Blend(average, quantity, movement.UnitCost, movement.Quantity);
                quantity += movement.Quantity;
            }
            else
            {
                quantity -= movement.Quantity;

                // Outbound movements do not change the average. A negative balance means more
                // was issued than received; the average is kept so the oversold part is visible
                // in the valuation instead of silently disappearing.
            }
        }

        return new StockPosition(quantity, average);
    }
}

/// <summary>Quantity on hand and its average unit cost, as of a point in the ledger.</summary>
/// <param name="Quantity">Signed quantity in base units.</param>
/// <param name="AverageUnitCost">Average cost per base unit, or <c>null</c> before any receipt.</param>
public sealed record StockPosition(decimal Quantity, Money? AverageUnitCost)
{
    public bool IsEmpty => Quantity == 0m;

    public bool IsNegative => Quantity < 0m;

    /// <summary>Stock value at average cost, or <c>null</c> when there is no cost basis yet.</summary>
    public Money? Value => AverageUnitCost?.Multiply(Quantity).Round(2);
}
