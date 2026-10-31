using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOpsHub.Application.Common;

namespace PropertyOpsHub.Api.Mapping;

/// <summary>
/// Translates application failures into RFC 9457 problem details.
/// </summary>
/// <remarks>
/// The status code comes from <see cref="ErrorKind"/> and the stable <c>code</c> is exposed as
/// an extension member, so clients branch on the code rather than parsing prose. The message is
/// always one the application layer wrote for a caller; exceptions are mapped separately.
/// </remarks>
public static class ErrorMapping
{
    public static int ToStatusCode(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    public static ProblemDetails ToProblemDetails(Error error, HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        var status = ToStatusCode(error.Kind);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(error.Kind),
            Detail = error.Message,
            Type = $"https://httpstatuses.io/{status}"
        };

        problem.Extensions["code"] = error.Code;

        if (error.FieldErrors.Count > 0)
        {
            problem.Extensions["errors"] = ToFieldDictionary(error.FieldErrors);
        }

        if (!string.IsNullOrEmpty(httpContext?.TraceIdentifier))
        {
            problem.Extensions["traceId"] = httpContext!.TraceIdentifier;
        }

        return problem;
    }

    /// <summary>Builds the 429 body used by the rate limiter, which has no <see cref="Error"/>.</summary>
    public static ProblemDetails TooManyRequests(string detail, int? retryAfterSeconds = null)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = detail,
            Type = "https://httpstatuses.io/429"
        };

        problem.Extensions["code"] = "rate_limit.exceeded";

        if (retryAfterSeconds is > 0)
        {
            problem.Extensions["retryAfterSeconds"] = retryAfterSeconds.Value;
        }

        return problem;
    }

    private static string TitleFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => "The request is invalid",
        ErrorKind.Unauthorized => "Authentication is required",
        ErrorKind.Forbidden => "The caller is not allowed to do that",
        ErrorKind.NotFound => "Not found",
        ErrorKind.Conflict => "The request conflicts with the current state",
        _ => "An unexpected error occurred"
    };

    private static Dictionary<string, string[]> ToFieldDictionary(IReadOnlyDictionary<string, string[]> fieldErrors)
    {
        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var (field, messages) in fieldErrors)
        {
            result[field] = messages;
        }

        return result;
    }
}
