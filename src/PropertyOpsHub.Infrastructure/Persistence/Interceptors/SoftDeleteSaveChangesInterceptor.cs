using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Turns <c>DELETE</c> into an update of <c>IsDeleted</c>. Financial history must stay
/// readable and journals must keep pointing at a row, so nothing is physically removed.
/// </summary>
/// <remarks>
/// Registered before the audit interceptor: once the state flips to <c>Modified</c>, the
/// audit interceptor stamps <c>ModifiedAtUtc</c> like any other change.
/// </remarks>
public sealed class SoftDeleteSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;

    public SoftDeleteSaveChangesInterceptor(TimeProvider timeProvider) =>
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplySoftDelete(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Deleted || entry.Entity is not ISoftDeletable)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;

            if (entry.Entity is IAuditableEntity)
            {
                entry.Property(nameof(IAuditableEntity.ModifiedAtUtc)).CurrentValue = now;
            }
        }
    }
}
