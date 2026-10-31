using System;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Domain.Organization;

/// <summary>
/// Tenant root: the owning company that holds one or more <see cref="Property"/> instances and
/// defines the reporting currency for consolidated reports.
/// </summary>
public sealed class Group : AggregateRoot<GroupId>, IAuditableEntity, ISoftDeletable
{
    public const int NameMaxLength = 120;

    private Group()
    {
        // Materialisation constructor for the persistence layer.
    }

    private Group(GroupId id, string name, CurrencyCode baseCurrency, DateTimeOffset createdAtUtc)
        : base(id)
    {
        Name = name;
        BaseCurrency = baseCurrency;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Currency that consolidated reports and inter-property journals are expressed in.</summary>
    public CurrencyCode BaseCurrency { get; private set; } = CurrencyCode.Create("USD");

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    /// <summary>Creates a group. Names are trimmed and must be non-empty.</summary>
    public static Group Create(string name, string baseCurrency, TimeProvider? timeProvider = null)
    {
        var clock = timeProvider ?? TimeProvider.System;

        var group = new Group(
            GroupId.New(),
            Guard.NotNullOrWhiteSpace(name, NameMaxLength),
            CurrencyCode.Create(baseCurrency),
            clock.GetUtcNow());

        group.RaiseDomainEvent(new GroupCreated(group.Id, group.Name));
        return group;
    }

    public void Rename(string name) => Name = Guard.NotNullOrWhiteSpace(name, NameMaxLength);

    /// <summary>
    /// Changes the reporting currency. Only permitted while no journals exist for the group;
    /// that check lives in the application layer because it needs persisted state.
    /// </summary>
    public void ChangeBaseCurrency(string baseCurrency) => BaseCurrency = CurrencyCode.Create(baseCurrency);

    /// <summary>Deactivates the group. Idempotent: repeated calls do not raise events.</summary>
    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    /// <summary>Soft-deletes the group; rows are retained for audit and history.</summary>
    public void Delete() => IsDeleted = true;
}
