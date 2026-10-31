using System;
using System.Collections.Generic;

namespace PropertyOpsHub.Application.Common;

/// <summary>How a failure should be surfaced to the caller.</summary>
public enum ErrorKind
{
    /// <summary>Input did not pass validation (HTTP 400).</summary>
    Validation = 0,

    /// <summary>The requested resource does not exist, or is not visible to this caller (HTTP 404).</summary>
    NotFound = 1,

    /// <summary>The request conflicts with current state (HTTP 409).</summary>
    Conflict = 2,

    /// <summary>The caller is authenticated but not allowed (HTTP 403).</summary>
    Forbidden = 3,

    /// <summary>The caller is not authenticated (HTTP 401).</summary>
    Unauthorized = 4,

    /// <summary>Anything unexpected (HTTP 500).</summary>
    Failure = 5
}

/// <summary>
/// A failure with a stable code, a safe message that can be shown to a client, and the
/// category the API layer maps to a status code. Never carries stack traces or SQL.
/// </summary>
public sealed class Error
{
    private static readonly IReadOnlyDictionary<string, string[]> NoFieldErrors =
        new Dictionary<string, string[]>(StringComparer.Ordinal);

    private Error(
        string code,
        string message,
        ErrorKind kind,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null)
    {
        Code = code;
        Message = message;
        Kind = kind;
        FieldErrors = fieldErrors ?? NoFieldErrors;
    }

    /// <summary>Machine-readable, for example <c>organization.property.code_duplicate</c>.</summary>
    public string Code { get; }

    /// <summary>Human-readable and safe to return to the caller.</summary>
    public string Message { get; }

    public ErrorKind Kind { get; }

    /// <summary>Per-field messages for <see cref="ErrorKind.Validation"/> failures.</summary>
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; }

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorKind.Validation);

    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new("validation.failed", "One or more validation errors occurred.", ErrorKind.Validation, fieldErrors);

    public static Error NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorKind.Forbidden);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorKind.Unauthorized);

    public static Error Failure(string code, string message) => new(code, message, ErrorKind.Failure);
}
