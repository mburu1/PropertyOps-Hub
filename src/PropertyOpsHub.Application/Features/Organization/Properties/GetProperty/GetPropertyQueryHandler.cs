using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Application.Common;
using PropertyOpsHub.Application.Security;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Application.Features.Organization.Properties.GetProperty;

/// <summary>
/// Loads one property for the caller.
/// </summary>
/// <remarks>
/// A caller without access gets "not found", not "forbidden": returning 403 would confirm
/// that the id exists, which is enough to enumerate another group's properties.
/// </remarks>
public sealed class GetPropertyQueryHandler : IRequestHandler<GetPropertyQuery, Result<PropertyDto>>
{
    private readonly IPropertyRepository _properties;
    private readonly ICurrentUser _currentUser;

    public GetPropertyQueryHandler(IPropertyRepository properties, ICurrentUser currentUser)
    {
        _properties = properties;
        _currentUser = currentUser;
    }

    public async Task<Result<PropertyDto>> Handle(GetPropertyQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUser.IsAuthenticated)
        {
            return Result.Failure<PropertyDto>(
                Error.Unauthorized("auth.unauthenticated", "Authentication is required."));
        }

        var property = await _properties.FindAsync(PropertyId.From(request.PropertyId), cancellationToken);

        if (property is null || property.IsDeleted)
        {
            return Result.Failure<PropertyDto>(
                Error.NotFound("organization.property.not_found", "The property does not exist."));
        }

        if (!_currentUser.CanAccessProperty(property.Id.Value))
        {
            return Result.Failure<PropertyDto>(
                Error.NotFound("organization.property.not_found", "The property does not exist."));
        }

        return Result.Success(PropertyDto.FromDomain(property));
    }
}
