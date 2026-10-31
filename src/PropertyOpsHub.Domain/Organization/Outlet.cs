using System;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.Organization;

/// <summary>
/// A revenue centre inside a <see cref="Property"/> (restaurant, bar, spa...) that POS sales
/// and stock movements are booked to.
/// </summary>
public sealed class Outlet : AggregateRoot<OutletId>, IAuditableEntity, ISoftDeletable
{
    public const int NameMaxLength = 120;

    private Outlet()
    {
        // Materialisation constructor for the persistence layer.
    }

    private Outlet(
        OutletId id,
        PropertyId propertyId,
        string name,
        OutletType type,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        PropertyId = propertyId;
        Name = name;
        Type = type;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public PropertyId PropertyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public OutletType Type { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    public static Outlet Create(
        PropertyId propertyId,
        string name,
        OutletType type,
        TimeProvider? timeProvider = null)
    {
        _ = Guard.NotEmpty(propertyId.Value, nameof(propertyId));
        _ = Guard.Defined(type, nameof(type));

        var clock = timeProvider ?? TimeProvider.System;

        var outlet = new Outlet(
            OutletId.New(),
            propertyId,
            Guard.NotNullOrWhiteSpace(name, NameMaxLength),
            type,
            clock.GetUtcNow());

        outlet.RaiseDomainEvent(new OutletCreated(outlet.Id, outlet.PropertyId, outlet.Type, outlet.Name));
        return outlet;
    }

    public void Rename(string name) => Name = Guard.NotNullOrWhiteSpace(name, NameMaxLength);

    public void ChangeType(OutletType type) => Type = Guard.Defined(type, nameof(type));

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Delete() => IsDeleted = true;
}
