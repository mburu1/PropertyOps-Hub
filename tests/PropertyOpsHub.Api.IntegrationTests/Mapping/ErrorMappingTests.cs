using System;
using Microsoft.AspNetCore.Http;
using PropertyOpsHub.Api.Mapping;
using PropertyOpsHub.Application.Common;

namespace PropertyOpsHub.Api.IntegrationTests.Mapping;

/// <summary>
/// The HTTP contract for failures: every application error becomes one RFC 9457 document with
/// a stable code and the status code implied by its kind.
/// </summary>
public class ErrorMappingTests
{
    [Theory]
    [InlineData(ErrorKind.Validation, 400)]
    [InlineData(ErrorKind.Unauthorized, 401)]
    [InlineData(ErrorKind.Forbidden, 403)]
    [InlineData(ErrorKind.NotFound, 404)]
    [InlineData(ErrorKind.Conflict, 409)]
    [InlineData(ErrorKind.Failure, 500)]
    public void Every_kind_maps_to_its_status_code(ErrorKind kind, int expected) =>
        Assert.Equal(expected, ErrorMapping.ToStatusCode(kind));

    [Fact]
    public void Conflict_keeps_the_application_code_and_message()
    {
        var error = Error.Conflict("organization.property.code_duplicate", "That code is taken.");

        var problem = ErrorMapping.ToProblemDetails(error);

        Assert.Equal(409, problem.Status);
        Assert.Equal("That code is taken.", problem.Detail);
        Assert.Equal("organization.property.code_duplicate", problem.Extensions["code"]);
        Assert.False(problem.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public void Validation_exposes_field_errors_for_the_client()
    {
        var error = Error.Validation(new Dictionary<string, string[]>
        {
            ["Name"] = ["A property name is required."],
            ["Code"] = ["Invalid.", "Too long."]
        });

        var problem = ErrorMapping.ToProblemDetails(error);

        Assert.Equal(400, problem.Status);
        var fields = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string[]>>(problem.Extensions["errors"]);
        Assert.Equal(2, fields.Count);
        Assert.Equal(2, fields["Code"].Length);
    }

    [Fact]
    public void Trace_identifier_is_included_when_available()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-123";

        var problem = ErrorMapping.ToProblemDetails(
            Error.NotFound("organization.property.not_found", "The property does not exist."),
            context);

        Assert.Equal("trace-123", problem.Extensions["traceId"]);
    }

    [Fact]
    public void Too_many_requests_carries_the_retry_hint()
    {
        var problem = ErrorMapping.TooManyRequests("Slow down.", retryAfterSeconds: 42);

        Assert.Equal(429, problem.Status);
        Assert.Equal("rate_limit.exceeded", problem.Extensions["code"]);
        Assert.Equal(42, problem.Extensions["retryAfterSeconds"]);
    }
}
