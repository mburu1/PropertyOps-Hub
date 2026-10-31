using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>Raised when a stock item is created.</summary>
public sealed record ItemCreated(ItemId ItemId, string Code, string Name) : DomainEvent;
