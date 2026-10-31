using System.Collections.Generic;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.Organization;

/// <summary>
/// Short, human-typed code for a property, unique within its group and used on exports and
/// journal references (for example <c>NBO-CBD</c>). Always stored upper case and trimmed.
/// </summary>
public sealed class PropertyCode : ValueObject
{
    public const int MinLength = 2;
    public const int MaxLength = 12;

    private PropertyCode(string value) => Value = value;

    public string Value { get; }

    public static PropertyCode Create(string? code)
    {
        if (!TryCreate(code, out var result))
        {
            throw new DomainException(
                "organization.property.code_invalid",
                $"Property code must be {MinLength}-{MaxLength} characters of letters, digits and hyphens.");
        }

        return result!;
    }

    /// <summary>
    /// Non-throwing variant used by request validators, where an invalid code is an expected
    /// outcome rather than an exceptional one.
    /// </summary>
    public static bool TryCreate(string? code, out PropertyCode? result)
    {
        result = null;
        var value = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (value.Length is < MinLength or > MaxLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterUpper(character) && !char.IsAsciiDigit(character) && character != '-')
            {
                return false;
            }
        }

        result = new PropertyCode(value);
        return true;
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
