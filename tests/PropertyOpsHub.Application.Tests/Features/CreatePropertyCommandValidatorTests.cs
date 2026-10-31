using System;
using System.Linq;
using PropertyOpsHub.Application.Features.Organization.Properties.CreateProperty;

namespace PropertyOpsHub.Application.Tests.Features;

public class CreatePropertyCommandValidatorTests
{
    private readonly CreatePropertyCommandValidator _validator = new();

    private static CreatePropertyCommand ValidCommand() =>
        new(Guid.CreateVersion7(), "Nairobi CBD", "NBO-CBD", "Africa/Nairobi");

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_group_id_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { GroupId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreatePropertyCommand.GroupId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_name_is_rejected(string name)
    {
        var result = _validator.Validate(ValidCommand() with { Name = name });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreatePropertyCommand.Name));
    }

    [Fact]
    public void Overlong_name_is_rejected()
    {
        var result = _validator.Validate(ValidCommand() with { Name = new string('a', 121) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreatePropertyCommand.Name));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("NBO CBD")]
    [InlineData("NBO_CBD")]
    [InlineData("ABCDEFGHIJKLM")]
    public void Malformed_code_is_rejected(string code)
    {
        var result = _validator.Validate(ValidCommand() with { Code = code });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreatePropertyCommand.Code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("not a time zone")]
    public void Unknown_time_zone_is_rejected(string timeZone)
    {
        var result = _validator.Validate(ValidCommand() with { TimeZoneId = timeZone });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreatePropertyCommand.TimeZoneId));
    }

    [Fact]
    public void Lower_case_code_is_accepted_because_it_is_normalised()
    {
        var result = _validator.Validate(ValidCommand() with { Code = "nbo-cbd" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Every_field_is_reported_in_one_pass()
    {
        var result = _validator.Validate(new CreatePropertyCommand(Guid.Empty, "", "!", ""));

        Assert.False(result.IsValid);
        var fields = result.Errors.Select(failure => failure.PropertyName).Distinct().ToList();
        Assert.Contains(nameof(CreatePropertyCommand.GroupId), fields);
        Assert.Contains(nameof(CreatePropertyCommand.Name), fields);
        Assert.Contains(nameof(CreatePropertyCommand.Code), fields);
        Assert.Contains(nameof(CreatePropertyCommand.TimeZoneId), fields);
    }
}
