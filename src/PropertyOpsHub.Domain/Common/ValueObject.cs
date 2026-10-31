using System;
using System.Collections.Generic;
using System.Linq;

namespace PropertyOpsHub.Domain.Common;

/// <summary>
/// Base class for immutable value objects: no identity, compared by the values it is made of.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Every value that participates in equality and hashing.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject valueObject && Equals(valueObject);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
