using System;

namespace PropertyOpsHub.Domain.Common;

/// <summary>
/// Something that happened in the domain and that other parts of the system may react to.
/// Domain events are raised by aggregates and dispatched after the transaction commits.
/// </summary>
public interface IDomainEvent
{
    /// <summary>When the event was raised, in UTC.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>
/// Convenience base for domain events. Records give value equality, which keeps events
/// comparable in tests.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
