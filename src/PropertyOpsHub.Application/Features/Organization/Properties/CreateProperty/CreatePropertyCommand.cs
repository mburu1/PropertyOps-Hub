using System;
using MediatR;
using PropertyOpsHub.Application.Common;

namespace PropertyOpsHub.Application.Features.Organization.Properties.CreateProperty;

/// <summary>Adds a property to a group. Returns the new property id.</summary>
public sealed record CreatePropertyCommand(
    Guid GroupId,
    string Name,
    string Code,
    string TimeZoneId = "UTC") : IRequest<Result<Guid>>;
