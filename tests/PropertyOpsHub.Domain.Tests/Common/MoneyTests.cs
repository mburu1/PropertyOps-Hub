using System;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.Tests.Common;

public class MoneyTests
{
    [Fact]
    public void Of_normalises_the_currency_code()
    {
        var money = Money.Of(12.5m, "  kes ");

        Assert.Equal("KES", money.Currency);
        Assert.Equal(12.5m, money.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("US1")]
    [InlineData("U S")]
    public void Of_rejects_codes_that_are_not_iso_4217(string currency)
    {
        var exception = Assert.Throws<DomainException>(() => Money.Of(1m, currency));

        Assert.Equal("finance.currency.invalid", exception.Code);
    }

    [Fact]
    public void Of_rejects_a_well_formed_but_unknown_currency()
    {
        var exception = Assert.Throws<DomainException>(() => Money.Of(1m, "KSH"));

        Assert.Equal("finance.currency.unknown", exception.Code);
    }

    [Fact]
    public void Add_combines_amounts_in_the_same_currency()
    {
        var total = Money.Of(10.25m, "USD").Add(Money.Of(4.75m, "usd"));

        Assert.Equal(15.00m, total.Amount);
        Assert.Equal("USD", total.Currency);
    }

    [Fact]
    public void Add_refuses_to_mix_currencies()
    {
        var exception = Assert.Throws<DomainException>(
            () => Money.Of(10m, "USD").Add(Money.Of(10m, "KES")));

        Assert.Equal("finance.money.currency_mismatch", exception.Code);
    }

    [Fact]
    public void Subtract_can_produce_a_negative_amount()
    {
        var result = Money.Of(10m, "EUR").Subtract(Money.Of(12m, "EUR"));

        Assert.Equal(-2m, result.Amount);
        Assert.True(result.IsNegative);
    }

    [Fact]
    public void Multiply_keeps_the_currency_and_the_exact_product()
    {
        var result = Money.Of(19.99m, "GBP").Multiply(3m);

        Assert.Equal(59.97m, result.Amount);
        Assert.Equal("GBP", result.Currency);
    }

    [Fact]
    public void Round_uses_bankers_rounding()
    {
        Assert.Equal(2.34m, Money.Of(2.345m, "USD").Round().Amount);
        Assert.Equal(2.36m, Money.Of(2.355m, "USD").Round().Amount);
    }

    [Fact]
    public void Equality_ignores_scale_and_currency_casing()
    {
        var left = Money.Of(10m, "usd");
        var right = Money.Of(10.00m, "USD");

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Zero_is_equal_for_the_same_currency_only()
    {
        Assert.True(Money.Zero("USD").IsZero);
        Assert.NotEqual(Money.Zero("USD"), Money.Zero("KES"));
    }

    [Fact]
    public void ToString_is_culture_invariant()
    {
        Assert.Equal("1250.50 USD", Money.Of(1250.5m, "USD").ToString());
    }
}
