using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.Organization;

/// <summary>Raised when a new tenant group is created.</summary>
public sealed record GroupCreated(GroupId GroupId, string Name) : DomainEvent;

/// <summary>Raised when a property is added to a group.</summary>
public sealed record PropertyCreated(
    PropertyId PropertyId,
    GroupId GroupId,
    string Code,
    string Name) : DomainEvent;

/// <summary>Raised when an outlet is added to a property.</summary>
public sealed record OutletCreated(
    OutletId OutletId,
    PropertyId PropertyId,
    OutletType Type,
    string Name) : DomainEvent;
