using System;
using System.Collections.Generic;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>Physical dimension a unit measures. Used for reporting and sanity checks.</summary>
public enum UomDimension
{
    Mass = 1,
    Volume = 2,
    Count = 3
}

/// <summary>
/// A unit of measure, normalised to upper case (for example <c>KG</c>, <c>ML</c>, <c>CASE</c>).
/// </summary>
/// <remarks>
/// Units are value objects, not rows: the set is small and stable, and an item owns the
/// conversions that turn its purchase unit into its base unit.
/// </remarks>
public sealed class UnitOfMeasure : ValueObject
{
    public const int MaxCodeLength = 8;

    private UnitOfMeasure(string code, UomDimension dimension)
    {
        Code = code;
        Dimension = dimension;
    }

    public string Code { get; }

    public UomDimension Dimension { get; }

    public static UnitOfMeasure Kilogram { get; } = new("KG", UomDimension.Mass);

    public static UnitOfMeasure Gram { get; } = new("G", UomDimension.Mass);

    public static UnitOfMeasure Litre { get; } = new("L", UomDimension.Volume);

    public static UnitOfMeasure Millilitre { get; } = new("ML", UomDimension.Volume);

    public static UnitOfMeasure Each { get; } = new("EA", UomDimension.Count);

    public static UnitOfMeasure Case { get; } = new("CASE", UomDimension.Count);

    public static UnitOfMeasure Create(string? code, UomDimension dimension)
    {
        var value = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (value.Length is < 1 or > MaxCodeLength)
        {
            throw new DomainException(
                "material.uom.code_invalid",
                $"A unit code must be 1-{MaxCodeLength} characters long.");
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character))
            {
                throw new DomainException(
                    "material.uom.code_invalid",
                    "A unit code may contain only letters and digits.");
            }
        }

        _ = Guard.Defined(dimension, nameof(dimension));

        return new UnitOfMeasure(value, dimension);
    }

    public override string ToString() => Code;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Dimension;
    }
}
