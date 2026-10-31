using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IGroupRepository"/>. Queries run through the context's
/// tenant filter, so a caller without group-wide rights gets <c>null</c> rather than a
/// forbidden-with-details answer.
/// </summary>
internal sealed class GroupRepository : IGroupRepository
{
    private readonly AppDbContext _context;

    public GroupRepository(AppDbContext context) => _context = context;

    public Task<Group?> FindAsync(GroupId id, CancellationToken cancellationToken) =>
        _context.Groups.FirstOrDefaultAsync(group => group.Id == id, cancellationToken);
}

/// <summary>EF Core implementation of <see cref="IPropertyRepository"/>.</summary>
internal sealed class PropertyRepository : IPropertyRepository
{
    private readonly AppDbContext _context;

    public PropertyRepository(AppDbContext context) => _context = context;

    public Task<Property?> FindAsync(PropertyId id, CancellationToken cancellationToken) =>
        _context.Properties.FirstOrDefaultAsync(property => property.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(
        GroupId groupId,
        PropertyCode code,
        CancellationToken cancellationToken) =>
        _context.Properties.AnyAsync(
            property => property.GroupId == groupId && property.Code == code,
            cancellationToken);

    public async Task<IReadOnlyList<Property>> ListByGroupAsync(
        GroupId groupId,
        CancellationToken cancellationToken) =>
        await _context.Properties
            .AsNoTracking()
            .Where(property => property.GroupId == groupId)
            .OrderBy(property => property.Name)
            .ToListAsync(cancellationToken);

    public void Add(Property property)
    {
        ArgumentNullException.ThrowIfNull(property);
        _context.Properties.Add(property);
    }
}

/// <summary>Commits the unit of work; one transaction per use case.</summary>
internal sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public EfUnitOfWork(AppDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
