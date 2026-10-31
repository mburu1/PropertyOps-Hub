using System;
using System.Collections.Generic;

namespace PropertyOpsHub.Domain.Common;

/// <summary>
/// Base class for every persisted object that has identity.
/// </summary>
/// <remarks>
/// Equality is identity based: two instances of the same CLR type with the same
/// <typeparamref name="TId"/> are the same entity. Aggregates always receive their id when
/// they are created, so the "two unsaved instances both have <c>default</c>" edge case does
/// not arise here.
/// </remarks>
/// <typeparam name="TId">Strongly typed identifier, for example <c>PropertyId</c>.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Creates an entity with a known identity.</summary>
    protected Entity(TId id) => Id = id;

    /// <summary>Materialisation constructor for object-relational mappers.</summary>
    protected Entity() => Id = default;

    /// <summary>Identity. Assigned once, in the constructor.</summary>
    public TId Id { get; protected set; }

    /// <summary>Events raised since the aggregate was loaded or created.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>Records a domain event; dispatched later, never inside the aggregate.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>Called by the dispatcher once events have been published.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other.GetType() == GetType()
            && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
