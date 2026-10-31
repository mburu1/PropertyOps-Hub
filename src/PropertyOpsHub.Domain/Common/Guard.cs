using System;
using System.Runtime.CompilerServices;

namespace PropertyOpsHub.Domain.Common;

/// <summary>
/// Argument checks that throw <see cref="DomainException"/> instead of
/// <see cref="ArgumentException"/>, so callers see one failure type for rule violations.
/// </summary>
/// <remarks>
/// The <c>[CallerArgumentExpression]</c> parameters mean call sites stay terse:
/// <c>Guard.NotNullOrWhiteSpace(name)</c> reports the parameter name automatically.
/// </remarks>
public static class Guard
{
    public static string NotNullOrWhiteSpace(
        string? value,
        int maxLength = 200,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("domain.value.required", $"{Label(name)} must not be empty.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                "domain.value.too_long",
                $"{Label(name)} must be at most {maxLength} characters; got {trimmed.Length}.");
        }

        return trimmed;
    }

    public static Guid NotEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("domain.value.required", $"{Label(name)} must not be empty.");
        }

        return value;
    }

    public static decimal NotNegative(
        decimal value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value < 0m)
        {
            throw new DomainException("domain.value.out_of_range", $"{Label(name)} must not be negative; got {value}.");
        }

        return value;
    }

    public static decimal Positive(
        decimal value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value <= 0m)
        {
            throw new DomainException("domain.value.out_of_range", $"{Label(name)} must be greater than zero; got {value}.");
        }

        return value;
    }

    public static TEnum Defined<TEnum>(TEnum value, [CallerArgumentExpression(nameof(value))] string? name = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException("domain.value.out_of_range", $"{Label(name)} has an undefined value: {value}.");
        }

        return value;
    }

    private static string Label(string? name) => string.IsNullOrWhiteSpace(name) ? "value" : name;
}
