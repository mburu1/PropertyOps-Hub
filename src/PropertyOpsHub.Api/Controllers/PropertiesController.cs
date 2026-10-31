using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PropertyOpsHub.Api.Extensions;
using PropertyOpsHub.Api.Mapping;
using PropertyOpsHub.Api.Security;
using PropertyOpsHub.Application.Common;
using PropertyOpsHub.Application.Features.Organization.Properties;
using PropertyOpsHub.Application.Features.Organization.Properties.CreateProperty;
using PropertyOpsHub.Application.Features.Organization.Properties.GetProperty;

namespace PropertyOpsHub.Api.Controllers;

/// <summary>
/// Property endpoints. Controllers stay thin: they translate HTTP to a request and a
/// <see cref="Result"/> back to HTTP, and nothing else.
/// </summary>
[ApiController]
[Route("api/properties")]
[Authorize(Policy = Policies.AuthenticatedUser)]
public sealed class PropertiesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IProblemDetailsService _problemDetailsService;

    public PropertiesController(ISender sender, IProblemDetailsService problemDetailsService)
    {
        _sender = sender;
        _problemDetailsService = problemDetailsService;
    }

    /// <summary>Adds a property to a group. Group finance or IT administration only.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ManageProperties)]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePropertyCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : await FailureAsync(result.Error!, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads one property the caller may see.</summary>
    [HttpGet("{id:guid}")]
    [EnableRateLimiting(RateLimitPolicies.Reads)]
    [ProducesResponseType<PropertyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPropertyQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : await FailureAsync(result.Error!, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the failure through <see cref="IProblemDetailsService"/>, the same path the
    /// exception handler uses. Going through MVC's ObjectResult would emit
    /// <c>application/json</c> rather than the <c>application/problem+json</c> RFC 9457 asks for.
    /// </summary>
    private async Task<IActionResult> FailureAsync(Error error, CancellationToken cancellationToken)
    {
        var problem = ErrorMapping.ToProblemDetails(error, HttpContext);

        HttpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = HttpContext,
            ProblemDetails = problem
        }).ConfigureAwait(false);

        return new EmptyResult();
    }
}
