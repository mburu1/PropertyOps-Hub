using System;
using System.Collections.Generic;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>
/// An amount expressed in a unit of measure. Used at the edges of the material control context
/// (purchasing, counts, issues); inside the stock ledger everything is already in base units.
/// </summary>
public sealed class Quantity : ValueObject
{
    private Quantity(decimal amount, UnitOfMeasure unit)
    {
        Amount = amount;
        Unit = unit;
    }

    public decimal Amount { get; }

    public UnitOfMeasure Unit { get; }

    public bool IsZero => Amount == 0m;

    public bool IsNegative => Amount < 0m;

    public static Quantity Of(decimal amount, UnitOfMeasure unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return new Quantity(amount, unit);
    }

    public static Quantity Zero(UnitOfMeasure unit) => Of(0m, unit);

    public Quantity Add(Quantity other)
    {
        EnsureSameUnit(other);
        return new Quantity(Amount + other.Amount, Unit);
    }

    public Quantity Subtract(Quantity other)
    {
        EnsureSameUnit(other);
        return new Quantity(Amount - other.Amount, Unit);
    }

    public Quantity Multiply(decimal factor) => new(Amount * factor, Unit);

    public override string ToString() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"{Amount:0.####} {Unit.Code}");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Unit;
    }

    private void EnsureSameUnit(Quantity other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Unit != other.Unit)
        {
            throw new DomainException(
                "material.quantity.unit_mismatch",
                $"Cannot combine {Unit.Code} with {other.Unit.Code}.");
        }
    }
}
