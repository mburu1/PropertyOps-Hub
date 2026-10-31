using System.Threading;
using System.Threading.Tasks;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Application.Abstractions;

/// <summary>Read and write access to <see cref="Group"/> aggregates.</summary>
public interface IGroupRepository
{
    /// <summary>Returns the group, or <c>null</c> when it does not exist or is soft-deleted.</summary>
    Task<Group?> FindAsync(GroupId id, CancellationToken cancellationToken);
}

/// <summary>Read and write access to <see cref="Property"/> aggregates.</summary>
public interface IPropertyRepository
{
    Task<Property?> FindAsync(PropertyId id, CancellationToken cancellationToken);

    /// <summary>True when the code is already used by another live property in the group.</summary>
    Task<bool> CodeExistsAsync(GroupId groupId, PropertyCode code, CancellationToken cancellationToken);

    Task<IReadOnlyList<Property>> ListByGroupAsync(GroupId groupId, CancellationToken cancellationToken);

    void Add(Property property);
}
