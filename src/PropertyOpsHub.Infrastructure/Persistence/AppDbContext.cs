using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Application.Security;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context. One context per bounded context would be the alternative;
/// a single context keeps cross-context reports and journals in one transaction, which this
/// domain needs.
/// </summary>
/// <remarks>
/// Two kinds of filter are applied here, and both fail closed:
/// <list type="bullet">
///   <item>soft delete: rows flagged <c>IsDeleted</c> are never returned;</item>
///   <item>tenancy: a caller only sees the properties in their claims, and an anonymous
///   context sees none of them. Background work must go through a system scope on purpose.</item>
/// </list>
/// Because the filters reference context members, EF re-evaluates them per instance, and
/// <see cref="IgnoreQueryFilters{TEntity}"/> is the explicit, greppable way to bypass them.
/// </remarks>
public sealed class AppDbContext : DbContext
{
    private readonly ICurrentUser _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(currentUser);

        _currentUser = currentUser;
    }

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<Outlet> Outlets => Set<Outlet>();

    /// <summary>True for group finance and IT administration; drives the tenant filters.</summary>
    public bool CanSeeAllProperties =>
        _currentUser.IsAuthenticated && _currentUser.CanSeeAllProperties();

    /// <summary>
    /// Property ids in the caller's claims, already converted to the strongly typed id so the
    /// filter compares converter output with converter output. EF translates this to an IN list.
    /// </summary>
    public PropertyId[] VisiblePropertyIds =>
        _currentUser.IsAuthenticated && !CanSeeAllProperties
            ? [.. _currentUser.PropertyIds.Select(PropertyId.From)]
            : [];

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Groups are the tenant root: only group-wide roles see them.
        modelBuilder.Entity<Group>().HasQueryFilter(group =>
            !group.IsDeleted && CanSeeAllProperties);

        modelBuilder.Entity<Property>().HasQueryFilter(property =>
            !property.IsDeleted && (CanSeeAllProperties || VisiblePropertyIds.Contains(property.Id)));

        modelBuilder.Entity<Outlet>().HasQueryFilter(outlet =>
            !outlet.IsDeleted && (CanSeeAllProperties || VisiblePropertyIds.Contains(outlet.PropertyId)));
    }
}
