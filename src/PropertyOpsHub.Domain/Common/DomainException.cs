using System;

namespace PropertyOpsHub.Domain.Common;

/// <summary>
/// Raised when an operation would leave the domain in an invalid state.
/// This is a programming or business-rule error, not a transport concern: the API layer
/// maps it to HTTP 409/422 rather than 500.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>Machine-readable code, stable enough for clients to branch on.</summary>
    public const string DefaultCode = "domain.rule_violated";

    public DomainException(string message)
        : base(message)
    {
        Code = DefaultCode;
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = DefaultCode;
    }

    public DomainException(string code, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    /// <summary>Machine-readable code, for example <c>organization.property.code_duplicate</c>.</summary>
    public string Code { get; }
}
