using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PropertyOpsHub.Application.Abstractions;

namespace PropertyOpsHub.Api.Security;

/// <summary>
/// Reads the caller from the validated JWT. Claims are read by their original names because
/// inbound claim mapping is disabled on the bearer handler, which keeps <c>sub</c>, <c>role</c>
/// and <c>property_id</c> exactly as the issuer wrote them.
/// </summary>
internal sealed class HttpContextCurrentUser : ICurrentUser
{
    public const string SubjectClaim = "sub";
    public const string EmailClaim = "email";
    public const string RoleClaim = "role";
    public const string PropertyClaim = "property_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(SubjectClaim), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(EmailClaim);

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(RoleClaim)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
        ?? [];

    public IReadOnlyCollection<Guid> PropertyIds =>
        Principal?.FindAll(PropertyClaim)
            .Select(claim => Guid.TryParse(claim.Value, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray()
        ?? [];
}
