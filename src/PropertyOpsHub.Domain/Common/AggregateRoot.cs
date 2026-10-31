using System;

namespace PropertyOpsHub.Domain.Common;

/// <summary>
/// An entity that is also a consistency boundary: it guards all of its invariants and is the
/// only thing repositories load and store.
/// </summary>
/// <typeparam name="TId">Strongly typed identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : struct, IEquatable<TId>
{
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    protected AggregateRoot()
    {
    }
}
