using System;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.Organization;

/// <summary>Identity of a <see cref="Group"/>: the tenant root above properties.</summary>
public readonly record struct GroupId(Guid Value)
{
    public static GroupId New() => new(Guid.CreateVersion7());

    public static GroupId From(Guid value) => new(Guard.NotEmpty(value, "groupId"));

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}

/// <summary>Identity of a <see cref="Property"/>: one hotel, resort or site.</summary>
public readonly record struct PropertyId(Guid Value)
{
    public static PropertyId New() => new(Guid.CreateVersion7());

    public static PropertyId From(Guid value) => new(Guard.NotEmpty(value, "propertyId"));

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}

/// <summary>Identity of an <see cref="Outlet"/>: a restaurant, bar or spa inside a property.</summary>
public readonly record struct OutletId(Guid Value)
{
    public static OutletId New() => new(Guid.CreateVersion7());

    public static OutletId From(Guid value) => new(Guard.NotEmpty(value, "outletId"));

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}
