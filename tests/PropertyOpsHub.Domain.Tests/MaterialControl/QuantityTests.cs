using System;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.MaterialControl;

namespace PropertyOpsHub.Domain.Tests.MaterialControl;

public class QuantityTests
{
    [Fact]
    public void Add_and_subtract_keep_the_unit()
    {
        var total = Quantity.Of(2.5m, UnitOfMeasure.Kilogram).Add(Quantity.Of(1.25m, UnitOfMeasure.Kilogram));

        Assert.Equal(3.75m, total.Amount);
        Assert.Equal(UnitOfMeasure.Kilogram, total.Unit);
    }

    [Fact]
    public void Combining_different_units_is_refused()
    {
        var exception = Assert.Throws<DomainException>(
            () => Quantity.Of(1m, UnitOfMeasure.Kilogram).Add(Quantity.Of(1m, UnitOfMeasure.Each)));

        Assert.Equal("material.quantity.unit_mismatch", exception.Code);
    }

    [Fact]
    public void Subtract_can_produce_a_negative_quantity()
    {
        var result = Quantity.Of(1m, UnitOfMeasure.Litre).Subtract(Quantity.Of(3m, UnitOfMeasure.Litre));

        Assert.Equal(-2m, result.Amount);
        Assert.True(result.IsNegative);
    }

    [Fact]
    public void Multiply_scales_the_amount_only()
    {
        var result = Quantity.Of(4m, UnitOfMeasure.Kilogram).Multiply(2.5m);

        Assert.Equal(10m, result.Amount);
        Assert.Equal(UnitOfMeasure.Kilogram, result.Unit);
    }

    [Fact]
    public void Equality_is_by_amount_and_unit()
    {
        Assert.Equal(Quantity.Of(2m, UnitOfMeasure.Kilogram), Quantity.Of(2.0m, UnitOfMeasure.Kilogram));
        Assert.NotEqual(Quantity.Of(2m, UnitOfMeasure.Kilogram), Quantity.Of(2m, UnitOfMeasure.Gram));
    }

    [Fact]
    public void Zero_is_zero_in_its_own_unit()
    {
        Assert.True(Quantity.Zero(UnitOfMeasure.Each).IsZero);
        Assert.Equal("0 EA", Quantity.Zero(UnitOfMeasure.Each).ToString());
    }

    [Fact]
    public void ToString_is_culture_invariant_and_trims_trailing_zeros()
    {
        Assert.Equal("12.5 KG", Quantity.Of(12.50m, UnitOfMeasure.Kilogram).ToString());
    }
}
