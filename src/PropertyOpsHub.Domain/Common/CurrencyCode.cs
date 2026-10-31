using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace PropertyOpsHub.Domain.Common;

/// <summary>
/// An ISO-4217 alphabetic currency code, normalised to upper case.
/// </summary>
/// <remarks>
/// Shape and membership are both checked. Membership comes from the platform's own culture
/// data (<see cref="RegionInfo.ISOCurrencySymbol"/>) instead of a hand-maintained table, so
/// typos such as <c>KSH</c> for <c>KES</c> are rejected. Under invariant globalization the
/// culture data is empty; validation then falls back to shape only rather than rejecting
/// every valid code.
/// </remarks>
public sealed class CurrencyCode : ValueObject
{
    private static readonly Lazy<HashSet<string>> KnownCodes = new(BuildKnownCodes, LazyThreadSafetyMode.ExecutionAndPublication);

    private CurrencyCode(string value) => Value = value;

    public string Value { get; }

    public static CurrencyCode Create(string? currency)
    {
        var value = (currency ?? string.Empty).Trim().ToUpperInvariant();

        if (value.Length != 3)
        {
            throw new DomainException("finance.currency.invalid", "Currency must be a three-letter ISO-4217 code.");
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterUpper(character))
            {
                throw new DomainException("finance.currency.invalid", $"'{value}' is not a valid ISO-4217 code.");
            }
        }

        var known = KnownCodes.Value;
        if (known.Count > 0 && !known.Contains(value))
        {
            throw new DomainException("finance.currency.unknown", $"'{value}' is not a known ISO-4217 currency.");
        }

        return new CurrencyCode(value);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static HashSet<string> BuildKnownCodes()
    {
        var codes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                var region = new RegionInfo(culture.Name);
                if (region.ISOCurrencySymbol.Length == 3)
                {
                    codes.Add(region.ISOCurrencySymbol);
                }
            }
            catch (ArgumentException)
            {
                // Cultures without a region (invariant, custom) contribute no currency.
            }
        }

        return codes;
    }
}
