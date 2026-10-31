using System;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.MaterialControl;

namespace PropertyOpsHub.Domain.Tests.MaterialControl;

public class UnitOfMeasureTests
{
    [Theory]
    [InlineData("kg", "KG")]
    [InlineData("  ml ", "ML")]
    [InlineData("case", "CASE")]
    public void Create_normalises_the_code(string input, string expected)
    {
        Assert.Equal(expected, UnitOfMeasure.Create(input, UomDimension.Mass).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("KILOGRAMS")]
    [InlineData("K G")]
    public void Create_rejects_codes_outside_the_allowed_shape(string code)
    {
        var exception = Assert.Throws<DomainException>(() => UnitOfMeasure.Create(code, UomDimension.Mass));

        Assert.Equal("material.uom.code_invalid", exception.Code);
    }

    [Fact]
    public void Create_rejects_an_undefined_dimension()
    {
        var exception = Assert.Throws<DomainException>(
            () => UnitOfMeasure.Create("BOX", (UomDimension)99));

        Assert.Equal("domain.value.out_of_range", exception.Code);
    }

    [Fact]
    public void Equality_includes_the_dimension()
    {
        var mass = UnitOfMeasure.Create("CS", UomDimension.Mass);
        var count = UnitOfMeasure.Create("CS", UomDimension.Count);

        Assert.NotEqual(mass, count);
        Assert.Equal(UnitOfMeasure.Kilogram, UnitOfMeasure.Create("kg", UomDimension.Mass));
    }

    [Fact]
    public void Known_units_have_the_expected_dimension()
    {
        Assert.Equal(UomDimension.Mass, UnitOfMeasure.Kilogram.Dimension);
        Assert.Equal(UomDimension.Volume, UnitOfMeasure.Litre.Dimension);
        Assert.Equal(UomDimension.Count, UnitOfMeasure.Each.Dimension);
    }
}
