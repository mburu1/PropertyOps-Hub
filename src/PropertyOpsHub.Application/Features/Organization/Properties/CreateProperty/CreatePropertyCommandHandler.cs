using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Application.Common;
using PropertyOpsHub.Application.Security;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Application.Features.Organization.Properties.CreateProperty;

/// <summary>
/// Creates a property inside a group.
/// </summary>
/// <remarks>
/// Order of checks matters: authorization first (no work for callers who may not act),
/// then existence, then uniqueness. Existence failures are reported as "not found" so a
/// caller cannot probe for group ids they cannot see.
/// </remarks>
public sealed class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Result<Guid>>
{
    private readonly IGroupRepository _groups;
    private readonly IPropertyRepository _properties;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreatePropertyCommandHandler(
        IGroupRepository groups,
        IPropertyRepository properties,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _groups = groups;
        _properties = properties;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUser.IsAuthenticated)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("auth.unauthenticated", "Authentication is required."));
        }

        if (!_currentUser.CanManageProperties())
        {
            return Result.Failure<Guid>(
                Error.Forbidden(
                    "organization.property.forbidden",
                    "Only group finance or IT administration may create properties."));
        }

        var groupId = GroupId.From(request.GroupId);

        var group = await _groups.FindAsync(groupId, cancellationToken);
        if (group is null || group.IsDeleted)
        {
            return Result.Failure<Guid>(
                Error.NotFound("organization.group.not_found", "The group does not exist."));
        }

        if (!group.IsActive)
        {
            return Result.Failure<Guid>(
                Error.Conflict("organization.group.inactive", "The group is deactivated."));
        }

        if (!PropertyCode.TryCreate(request.Code, out var code))
        {
            // The validator normally catches this; keep the invariant if a handler is called directly.
            return Result.Failure<Guid>(
                Error.Validation("organization.property.code_invalid", "The property code is not valid."));
        }

        if (await _properties.CodeExistsAsync(groupId, code!, cancellationToken))
        {
            return Result.Failure<Guid>(
                Error.Conflict(
                    "organization.property.code_duplicate",
                    $"Property code '{code}' is already used in this group."));
        }

        try
        {
            var property = Property.Create(groupId, request.Name, code!.Value, request.TimeZoneId, _timeProvider);

            _properties.Add(property);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(property.Id.Value);
        }
        catch (DomainException exception)
        {
            // A rule the validator could not know about (for example length after trimming).
            return Result.Failure<Guid>(Error.Conflict(exception.Code, exception.Message));
        }
    }
}
