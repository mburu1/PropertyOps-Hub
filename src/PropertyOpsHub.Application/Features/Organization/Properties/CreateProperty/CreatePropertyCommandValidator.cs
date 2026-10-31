using System;
using FluentValidation;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Application.Features.Organization.Properties.CreateProperty;

/// <summary>
/// Input contract for <see cref="CreatePropertyCommand"/>. Runs before the handler, so the
/// handler only has to deal with business outcomes.
/// </summary>
public sealed class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(command => command.GroupId)
            .NotEmpty()
            .WithMessage("A group id is required.");

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("A property name is required.")
            .MaximumLength(Property.NameMaxLength)
            .WithMessage($"A property name may be at most {Property.NameMaxLength} characters.");

        RuleFor(command => command.Code)
            .NotEmpty()
            .WithMessage("A property code is required.")
            .Must(BeAValidCode)
            .WithMessage(
                $"A property code must be {PropertyCode.MinLength}-{PropertyCode.MaxLength} characters of letters, digits and hyphens.");

        RuleFor(command => command.TimeZoneId)
            .NotEmpty()
            .WithMessage("A time zone is required.")
            .MaximumLength(Property.TimeZoneIdMaxLength)
            .WithMessage($"A time zone may be at most {Property.TimeZoneIdMaxLength} characters.")
            .Must(BeAKnownTimeZone)
            .WithMessage("The time zone is not recognised; use an IANA id such as 'Africa/Nairobi'.");
    }

    private static bool BeAValidCode(string? code) => PropertyCode.TryCreate(code, out _);

    private static bool BeAKnownTimeZone(string? timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId)
        && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
