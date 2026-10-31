using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PropertyOpsHub.Application.Common.Exceptions;
using PropertyOpsHub.Api.Logging;
using PropertyOpsHub.Api.Mapping;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Api.ErrorHandling;

/// <summary>
/// Converts unhandled exceptions into the same problem-details shape the controllers use, so a
/// client only ever parses one error format.
/// </summary>
/// <remarks>
/// Unexpected exceptions are logged in full and answered with a fixed message: stack traces,
/// SQL text and connection strings must never reach a response.
/// </remarks>
internal sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private const string UnexpectedMessage = "An unexpected error occurred. Quote the trace id when reporting it.";

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => FromValidation(httpContext, validation),
            DomainException domain => FromDomain(httpContext, domain),
            _ => FromUnexpected(httpContext, exception)
        };

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        }).ConfigureAwait(false);

        return true;
    }

    private static ProblemDetails FromValidation(HttpContext httpContext, ValidationException exception)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The request is invalid",
            Detail = "One or more validation errors occurred.",
            Type = "https://httpstatuses.io/400"
        };

        problem.Extensions["code"] = "validation.failed";
        problem.Extensions["errors"] = exception.Errors;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return problem;
    }

    private static ProblemDetails FromDomain(HttpContext httpContext, DomainException exception)
    {
        // A domain rule that a handler did not translate: the caller asked for something the
        // business forbids, which is a conflict rather than a server fault.
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The request conflicts with the current state",
            Detail = exception.Message,
            Type = "https://httpstatuses.io/409"
        };

        problem.Extensions["code"] = exception.Code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return problem;
    }

    private ProblemDetails FromUnexpected(HttpContext httpContext, Exception exception)
    {
        ApiLogMessages.UnhandledException(
            _logger,
            exception,
            httpContext.Request.Method,
            httpContext.Request.Path.Value ?? "/",
            httpContext.TraceIdentifier);
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = UnexpectedMessage,
            Type = "https://httpstatuses.io/500"
        };

        problem.Extensions["code"] = "server.unexpected_error";
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return problem;
    }
}
