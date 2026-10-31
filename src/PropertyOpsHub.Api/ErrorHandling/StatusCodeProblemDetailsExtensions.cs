using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOpsHub.Api.Mapping;

namespace PropertyOpsHub.Api.ErrorHandling;

/// <summary>
/// Gives protocol-level failures a body. The authentication handler answers 401 and the
/// authorization middleware answers 403 with no content, which leaves a client parsing an
/// empty response; this fills in the same problem-details shape the rest of the API uses.
/// </summary>
/// <remarks>
/// Only runs when the response is still empty, so a controller or the exception handler that
/// already wrote a body is never overwritten.
/// </remarks>
internal static class StatusCodeProblemDetailsExtensions
{
    /// <summary>Web defaults, so the payload matches the casing MVC produces.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IApplicationBuilder UseProblemDetailsForStatusCodes(this IApplicationBuilder app)
    {
        return app.UseStatusCodePages(context => WriteProblemAsync(context.HttpContext));
    }

    private static Task WriteProblemAsync(HttpContext httpContext)
    {
        var problem = httpContext.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => Build(
                StatusCodes.Status401Unauthorized,
                "auth.unauthenticated",
                "Authentication is required",
                "A valid bearer credential is required for this endpoint."),

            StatusCodes.Status403Forbidden => Build(
                StatusCodes.Status403Forbidden,
                "auth.forbidden",
                "The caller is not allowed to do that",
                "The caller is authenticated but lacks the role this endpoint requires."),

            StatusCodes.Status404NotFound => Build(
                StatusCodes.Status404NotFound,
                "endpoint.not_found",
                "Not found",
                "No endpoint matches this route."),

            StatusCodes.Status405MethodNotAllowed => Build(
                StatusCodes.Status405MethodNotAllowed,
                "endpoint.method_not_allowed",
                "Method not allowed",
                "This route does not support the requested method."),

            _ => null
        };

        if (problem is null)
        {
            return Task.CompletedTask;
        }

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        // Written directly: the JSON helper would force application/json, and RFC 9457 asks
        // for application/problem+json.
        httpContext.Response.ContentType = "application/problem+json; charset=utf-8";

        return httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonOptions),
            httpContext.RequestAborted);
    }

    private static ProblemDetails Build(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{status}"
        };

        problem.Extensions["code"] = code;

        return problem;
    }
}
