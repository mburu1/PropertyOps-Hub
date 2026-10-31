using System;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>
/// One line of the stock ledger. Append only: entries are never edited or removed, so the
/// balance of an item is always the sum of its history (ADR 0003).
/// </summary>
/// <remarks>
/// <para>
/// Quantities are stored in the item's base unit and always as a positive magnitude; the
/// direction comes from <see cref="Type"/>. Storing a signed number as well would let the two
/// disagree.
/// </para>
/// <para>
/// <see cref="UnitCost"/> is the cost per base unit at the moment of the movement. Keeping the
/// snapshot means a later price change cannot rewrite the value of past stock.
/// </para>
/// </remarks>
public sealed class StockMovement : AggregateRoot<StockMovementId>, IAuditableEntity
{
    public const int ReferenceMaxLength = 64;

    private StockMovement()
    {
        // Materialisation constructor for the persistence layer.
    }

    private StockMovement(
        StockMovementId id,
        PropertyId propertyId,
        ItemId itemId,
        StockMovementType type,
        decimal quantity,
        Money unitCost,
        DateOnly businessDate,
        string reference,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        PropertyId = propertyId;
        ItemId = itemId;
        Type = type;
        Quantity = quantity;
        UnitCost = unitCost;
        BusinessDate = businessDate;
        Reference = reference;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Tenancy: stock belongs to one property and is never summed across them.</summary>
    public PropertyId PropertyId { get; private set; }

    public ItemId ItemId { get; private set; }

    public StockMovementType Type { get; private set; }

    /// <summary>Positive magnitude in the item's base unit.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Cost per base unit at the time of the movement.</summary>
    public Money UnitCost { get; private set; } = Money.Zero("USD");

    /// <summary>Business date, used for period reporting; not the same as the record time.</summary>
    public DateOnly BusinessDate { get; private set; }

    /// <summary>Human reference such as a goods receipt or count number.</summary>
    public string Reference { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Ledger entries are immutable, so this is always null.</summary>
    public DateTimeOffset? ModifiedAtUtc => null;

    /// <summary>Quantity with its direction applied; the ledger sums these.</summary>
    public decimal SignedQuantity => Type.IsInbound() ? Quantity : -Quantity;

    public static StockMovement Record(
        PropertyId propertyId,
        ItemId itemId,
        StockMovementType type,
        decimal quantity,
        Money unitCost,
        DateOnly businessDate,
        string reference,
        TimeProvider? timeProvider = null)
    {
        _ = Guard.NotEmpty(propertyId.Value, nameof(propertyId));
        _ = Guard.NotEmpty(itemId.Value, nameof(itemId));
        _ = Guard.Defined(type, nameof(type));
        _ = Guard.Positive(quantity, nameof(quantity));

        ArgumentNullException.ThrowIfNull(unitCost);

        if (unitCost.IsNegative)
        {
            throw new DomainException("material.stock.cost_negative", "Unit cost must not be negative.");
        }

        if (businessDate == default)
        {
            throw new DomainException("material.stock.business_date_required", "A business date is required.");
        }

        var clock = timeProvider ?? TimeProvider.System;

        return new StockMovement(
            StockMovementId.New(),
            propertyId,
            itemId,
            type,
            quantity,
            unitCost,
            businessDate,
            Guard.NotNullOrWhiteSpace(reference, ReferenceMaxLength),
            clock.GetUtcNow());
    }
}
