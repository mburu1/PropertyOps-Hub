using System;
using System.Collections.Generic;
using System.Globalization;

namespace PropertyOpsHub.Domain.Common;

/// <summary>
/// A monetary amount in exactly one ISO-4217 currency. Immutable; every operation returns a
/// new instance and refuses to mix currencies, which is the most common source of silent
/// accounting errors.
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>Minor-unit precision used when rounding for presentation or journal lines.</summary>
    public const int MinorUnitDecimals = 2;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    /// <summary>Uppercase ISO-4217 alphabetic code, for example <c>KES</c>.</summary>
    public string Currency { get; }

    public bool IsZero => Amount == 0m;

    public bool IsNegative => Amount < 0m;

    public static Money Of(decimal amount, string currency) => new(amount, CurrencyCode.Create(currency).Value);

    public static Money Zero(string currency) => new(0m, CurrencyCode.Create(currency).Value);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    /// <summary>Rounds to the currency's minor unit using banker's rounding.</summary>
    public Money Round(int decimals = MinorUnitDecimals) =>
        new(decimal.Round(Amount, decimals, MidpointRounding.ToEven), Currency);

    public override string ToString() =>
        string.Concat(Amount.ToString("0.00##", CultureInfo.InvariantCulture), " ", Currency);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new DomainException(
                "finance.money.currency_mismatch",
                $"Cannot combine {Currency} with {other.Currency}.");
        }
    }
}
