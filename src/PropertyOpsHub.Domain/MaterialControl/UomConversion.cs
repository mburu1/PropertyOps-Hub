using System;
using System.Collections.Generic;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>
/// Item-specific conversion from a purchase or issue unit to the item's base unit.
/// </summary>
/// <remarks>
/// One <c>CASE</c> of a given item is twelve kilograms, but a different item's case is six.
/// That is why the factor lives on the item and not on the unit: units are shared, factors are
/// not. The base unit is implicit, so the conversion only stores the "from" side.
/// </remarks>
public sealed class UomConversion : ValueObject
{
    private UomConversion(UnitOfMeasure from, decimal factor)
    {
        From = from;
        Factor = factor;
    }

    /// <summary>The unit being converted from.</summary>
    public UnitOfMeasure From { get; }

    /// <summary>How many base units one <see cref="From"/> is worth. Always greater than zero.</summary>
    public decimal Factor { get; }

    /// <summary>Creates a conversion whose result is one <see cref="From"/> in base units.</summary>
    public static UomConversion Create(UnitOfMeasure from, decimal factor)
    {
        ArgumentNullException.ThrowIfNull(from);
        _ = Guard.Positive(factor, nameof(factor));

        return new UomConversion(from, factor);
    }

    public override string ToString() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"1 {From.Code} = {Factor:0.####}");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return From;
        yield return Factor;
    }
}
