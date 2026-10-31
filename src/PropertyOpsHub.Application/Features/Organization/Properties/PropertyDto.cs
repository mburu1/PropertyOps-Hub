using System;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Application.Features.Organization.Properties;

/// <summary>Read model for a property. Never exposes persistence types.</summary>
public sealed record PropertyDto(
    Guid Id,
    Guid GroupId,
    string Name,
    string Code,
    string TimeZoneId,
    bool IsActive)
{
    public static PropertyDto FromDomain(Property property)
    {
        ArgumentNullException.ThrowIfNull(property);

        return new PropertyDto(
            property.Id.Value,
            property.GroupId.Value,
            property.Name,
            property.Code.Value,
            property.TimeZoneId,
            property.IsActive);
    }
}
