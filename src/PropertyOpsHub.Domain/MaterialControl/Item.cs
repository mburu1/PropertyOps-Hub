using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>
/// A stock item: what it is, how it is measured, and how its purchase units convert to the
/// base unit that the ledger records.
/// </summary>
/// <remarks>
/// Conversions belong to the item because "one case" means twelve kilograms for one product and
/// six for another. Quantities are normalised to the base unit before they reach the ledger, so
/// stock maths never has to guess which unit a number is in.
/// </remarks>
public sealed class Item : AggregateRoot<ItemId>, IAuditableEntity, ISoftDeletable
{
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 160;
    public const int CodeMinLength = 2;

    private readonly List<UomConversion> _conversions = [];

    private Item()
    {
        // Materialisation constructor for the persistence layer.
    }

    private Item(
        ItemId id,
        string code,
        string name,
        ItemCategory category,
        UnitOfMeasure baseUnit,
        CostingMethod costingMethod,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Code = code;
        Name = name;
        Category = category;
        BaseUnit = baseUnit;
        CostingMethod = costingMethod;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Short code used on purchase orders and reports, unique per property.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public ItemCategory Category { get; private set; }

    /// <summary>The unit every ledger entry is expressed in.</summary>
    public UnitOfMeasure BaseUnit { get; private set; } = UnitOfMeasure.Each;

    public CostingMethod CostingMethod { get; private set; }

    /// <summary>Purchase and issue units, each with its factor to <see cref="BaseUnit"/>.</summary>
    public IReadOnlyList<UomConversion> Conversions => _conversions;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    public static Item Create(
        string code,
        string name,
        ItemCategory category,
        UnitOfMeasure baseUnit,
        CostingMethod costingMethod = CostingMethod.WeightedAverage,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(baseUnit);
        _ = Guard.Defined(category, nameof(category));
        _ = Guard.Defined(costingMethod, nameof(costingMethod));

        var clock = timeProvider ?? TimeProvider.System;

        var item = new Item(
            ItemId.New(),
            NormaliseCode(code),
            Guard.NotNullOrWhiteSpace(name, NameMaxLength),
            category,
            baseUnit,
            costingMethod,
            clock.GetUtcNow());

        item.RaiseDomainEvent(new ItemCreated(item.Id, item.Code, item.Name));
        return item;
    }

    public void Rename(string name) => Name = Guard.NotNullOrWhiteSpace(name, NameMaxLength);

    public void Recategorize(ItemCategory category) => Category = Guard.Defined(category, nameof(category));

    /// <summary>
    /// Adds a purchase or issue unit. The base unit cannot be converted to itself, and a unit
    /// may only appear once, so conversions are unambiguous.
    /// </summary>
    public void AddConversion(UnitOfMeasure from, decimal factor)
    {
        ArgumentNullException.ThrowIfNull(from);

        if (from == BaseUnit)
        {
            throw new DomainException(
                "material.item.conversion_to_base",
                $"The base unit {BaseUnit.Code} does not need a conversion.");
        }

        if (_conversions.Any(conversion => conversion.From == from))
        {
            throw new DomainException(
                "material.item.conversion_duplicate",
                $"A conversion from {from.Code} already exists.");
        }

        _conversions.Add(UomConversion.Create(from, factor));
    }

    public void RemoveConversion(UnitOfMeasure from)
    {
        ArgumentNullException.ThrowIfNull(from);

        var existing = _conversions.FirstOrDefault(conversion => conversion.From == from)
            ?? throw new DomainException(
                "material.item.conversion_missing",
                $"There is no conversion from {from.Code}.");

        _conversions.Remove(existing);
    }

    /// <summary>Base units per one <paramref name="from"/>. The base unit converts to one.</summary>
    public decimal FactorFor(UnitOfMeasure from)
    {
        ArgumentNullException.ThrowIfNull(from);

        if (from == BaseUnit)
        {
            return 1m;
        }

        var conversion = _conversions.FirstOrDefault(candidate => candidate.From == from)
            ?? throw new DomainException(
                "material.item.conversion_missing",
                $"No conversion is defined from {from.Code} to {BaseUnit.Code}.");

        return conversion.Factor;
    }

    /// <summary>Re-expresses an amount of <paramref name="from"/> in the base unit.</summary>
    public decimal ToBaseUnits(decimal amount, UnitOfMeasure from) => amount * FactorFor(from);

    /// <summary>Re-expresses a quantity in the base unit.</summary>
    public Quantity ToBase(Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(quantity);

        return Quantity.Of(ToBaseUnits(quantity.Amount, quantity.Unit), BaseUnit);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Delete() => IsDeleted = true;

    private static string NormaliseCode(string? code)
    {
        var value = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (value.Length is < CodeMinLength or > CodeMaxLength)
        {
            throw new DomainException(
                "material.item.code_invalid",
                $"An item code must be {CodeMinLength}-{CodeMaxLength} characters long.");
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_')
            {
                throw new DomainException(
                    "material.item.code_invalid",
                    "An item code may contain only letters, digits, hyphens and underscores.");
            }
        }

        return value;
    }

    /// <summary>Readable form for logs and test output.</summary>
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Code} ({BaseUnit.Code})");
}
