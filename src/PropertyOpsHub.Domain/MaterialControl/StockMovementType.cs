using System;

namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>
/// Why stock moved. Values are persisted, so a number must never be reused.
/// </summary>
public enum StockMovementType
{
    /// <summary>Goods received against a purchase order.</summary>
    GoodsReceipt = 1,

    /// <summary>Issued to a kitchen, bar or outlet.</summary>
    Issue = 2,

    /// <summary>Spoilage, breakage, staff meals.</summary>
    Wastage = 3,

    /// <summary>Positive correction after a stock count.</summary>
    AdjustmentIn = 4,

    /// <summary>Negative correction after a stock count.</summary>
    AdjustmentOut = 5,

    /// <summary>Received from another property.</summary>
    TransferIn = 6,

    /// <summary>Sent to another property.</summary>
    TransferOut = 7
}

/// <summary>Direction rules for <see cref="StockMovementType"/>.</summary>
public static class StockMovementTypeExtensions
{
    /// <summary>True when the movement adds stock.</summary>
    public static bool IsInbound(this StockMovementType type) => type is
        StockMovementType.GoodsReceipt or
        StockMovementType.AdjustmentIn or
        StockMovementType.TransferIn;
}
