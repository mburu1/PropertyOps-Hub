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
        var value = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (value.Length is < MinLength or > MaxLength)
        {
            throw new DomainException(
                "organization.property.code_invalid",
                $"Property code must be {MinLength}-{MaxLength} characters long.");
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterUpper(character) && !char.IsAsciiDigit(character) && character != '-')
            {
                throw new DomainException(
                    "organization.property.code_invalid",
                    "Property code may contain only letters, digits and hyphens.");
            }
        }

        return new PropertyCode(value);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
