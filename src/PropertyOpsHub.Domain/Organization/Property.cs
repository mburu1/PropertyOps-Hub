using System;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.Organization;

/// <summary>
/// A physical property (hotel, resort or site) owned by a <see cref="Group"/>. This is the
/// tenancy boundary: every operational row in the system carries this id.
/// </summary>
public sealed class Property : AggregateRoot<PropertyId>, IAuditableEntity, ISoftDeletable
{
    public const int NameMaxLength = 120;
    public const int TimeZoneIdMaxLength = 64;

    private Property()
    {
        // Materialisation constructor for the persistence layer.
    }

    private Property(
        PropertyId id,
        GroupId groupId,
        string name,
        PropertyCode code,
        string timeZoneId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        GroupId = groupId;
        Name = name;
        Code = code;
        TimeZoneId = timeZoneId;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public GroupId GroupId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Short code used on exports; unique per group and enforced by a unique index.</summary>
    public PropertyCode Code { get; private set; } = PropertyCode.Create("NA");

    /// <summary>
    /// IANA time zone used to decide the business date of POS and stock transactions
    /// (for example <c>Africa/Nairobi</c>). The API validates the value against
    /// <see cref="TimeZoneInfo"/> before it reaches the aggregate.
    /// </summary>
    public string TimeZoneId { get; private set; } = "UTC";

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    public static Property Create(
        GroupId groupId,
        string name,
        string code,
        string timeZoneId = "UTC",
        TimeProvider? timeProvider = null)
    {
        _ = Guard.NotEmpty(groupId.Value, nameof(groupId));

        var clock = timeProvider ?? TimeProvider.System;

        var property = new Property(
            PropertyId.New(),
            groupId,
            Guard.NotNullOrWhiteSpace(name, NameMaxLength),
            PropertyCode.Create(code),
            Guard.NotNullOrWhiteSpace(timeZoneId, TimeZoneIdMaxLength),
            clock.GetUtcNow());

        property.RaiseDomainEvent(new PropertyCreated(property.Id, property.GroupId, property.Code.ToString(), property.Name));
        return property;
    }

    public void Rename(string name) => Name = Guard.NotNullOrWhiteSpace(name, NameMaxLength);

    public void ChangeCode(PropertyCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        Code = code;
    }

    public void ChangeTimeZone(string timeZoneId) =>
        TimeZoneId = Guard.NotNullOrWhiteSpace(timeZoneId, TimeZoneIdMaxLength);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Delete() => IsDeleted = true;
}
