using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Application.Tests.Fakes;

/// <summary>In-memory group store; behaves like the EF repository for the checks under test.</summary>
internal sealed class FakeGroupRepository : IGroupRepository
{
    private readonly List<Group> _groups = [];

    public int FindCount { get; private set; }

    public void Add(Group group) => _groups.Add(group);

    public Task<Group?> FindAsync(GroupId id, CancellationToken cancellationToken)
    {
        FindCount++;
        return Task.FromResult(_groups.SingleOrDefault(group => group.Id == id));
    }
}

/// <summary>In-memory property store with the same semantics as the EF implementation.</summary>
internal sealed class FakePropertyRepository : IPropertyRepository
{
    private readonly List<Property> _properties = [];

    public IReadOnlyList<Property> Items => _properties;

    public void Add(Property property) => _properties.Add(property);

    public Task<Property?> FindAsync(PropertyId id, CancellationToken cancellationToken) =>
        Task.FromResult(_properties.SingleOrDefault(property => property.Id == id));

    public Task<bool> CodeExistsAsync(GroupId groupId, PropertyCode code, CancellationToken cancellationToken) =>
        Task.FromResult(_properties.Any(property =>
            property.GroupId == groupId && property.Code == code && !property.IsDeleted));

    public Task<IReadOnlyList<Property>> ListByGroupAsync(GroupId groupId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Property>>(
            _properties.Where(property => property.GroupId == groupId && !property.IsDeleted).ToList());
}

/// <summary>Counts commits so tests can prove a use case persisted exactly once.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(SaveCount);
    }
}
