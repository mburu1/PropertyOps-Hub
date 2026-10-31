using System;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.MaterialControl;

namespace PropertyOpsHub.Domain.Tests.MaterialControl;

public class UomConversionTests
{
    [Fact]
    public void Create_stores_the_factor_to_the_base_unit()
    {
        var conversion = UomConversion.Create(UnitOfMeasure.Case, 12m);

        Assert.Equal(UnitOfMeasure.Case, conversion.From);
        Assert.Equal(12m, conversion.Factor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_a_factor_that_is_not_positive(decimal factor)
    {
        var exception = Assert.Throws<DomainException>(
            () => UomConversion.Create(UnitOfMeasure.Case, factor));

        Assert.Equal("domain.value.out_of_range", exception.Code);
    }

    [Fact]
    public void Create_rejects_a_null_unit()
    {
        Assert.Throws<ArgumentNullException>(() => UomConversion.Create(null!, 1m));
    }

    [Fact]
    public void Equality_is_by_unit_and_factor()
    {
        Assert.Equal(
            UomConversion.Create(UnitOfMeasure.Case, 12m),
            UomConversion.Create(UnitOfMeasure.Case, 12.0m));

        Assert.NotEqual(
            UomConversion.Create(UnitOfMeasure.Case, 12m),
            UomConversion.Create(UnitOfMeasure.Case, 24m));
    }

    [Fact]
    public void ToString_reads_as_a_sentence()
    {
        Assert.Equal("1 CASE = 12", UomConversion.Create(UnitOfMeasure.Case, 12m).ToString());
    }
}
