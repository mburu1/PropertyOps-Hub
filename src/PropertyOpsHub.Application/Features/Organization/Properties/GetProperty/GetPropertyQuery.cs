using System;
using MediatR;
using PropertyOpsHub.Application.Common;

namespace PropertyOpsHub.Application.Features.Organization.Properties.GetProperty;

/// <summary>Reads a single property the caller is allowed to see.</summary>
public sealed record GetPropertyQuery(Guid PropertyId) : IRequest<Result<PropertyDto>>;
