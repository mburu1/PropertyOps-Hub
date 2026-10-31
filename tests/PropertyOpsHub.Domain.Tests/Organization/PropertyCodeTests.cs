using System;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Domain.Tests.Organization;

public class PropertyCodeTests
{
    [Theory]
    [InlineData("nbo-cbd", "NBO-CBD")]
    [InlineData("  Nbo-Cbd  ", "NBO-CBD")]
    [InlineData("ab", "AB")]
    [InlineData("karen2", "KAREN2")]
    public void Create_normalises_to_upper_case_and_trims(string input, string expected)
    {
        Assert.Equal(expected, PropertyCode.Create(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("ABCDEFGHIJKLM")]
    [InlineData("NBO CBD")]
    [InlineData("NBO_CBD")]
    [InlineData("NBO.CBD")]
    [InlineData("NAIROBÓ")]
    public void Create_rejects_codes_outside_the_allowed_shape(string input)
    {
        var exception = Assert.Throws<PropertyOpsHub.Domain.Common.DomainException>(
            () => PropertyCode.Create(input));

        Assert.Equal("organization.property.code_invalid", exception.Code);
    }

    [Fact]
    public void Equality_is_based_on_the_normalised_value()
    {
        var left = PropertyCode.Create("nbo-cbd");
        var right = PropertyCode.Create("NBO-CBD");

        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void ToString_returns_the_stored_value()
    {
        Assert.Equal("MSA-BEACH", PropertyCode.Create("msa-beach").ToString());
    }
}
