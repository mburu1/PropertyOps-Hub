using System;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>Identity of an <see cref="Item"/>.</summary>
public readonly record struct ItemId(Guid Value)
{
    public static ItemId New() => new(Guid.CreateVersion7());

    public static ItemId From(Guid value) => new(Guard.NotEmpty(value, "itemId"));

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}

/// <summary>Identity of a <see cref="StockMovement"/> entry.</summary>
public readonly record struct StockMovementId(Guid Value)
{
    public static StockMovementId New() => new(Guid.CreateVersion7());

    public static StockMovementId From(Guid value) => new(Guard.NotEmpty(value, "stockMovementId"));

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}
