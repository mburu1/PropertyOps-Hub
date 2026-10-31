using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PropertyOpsHub.Domain.Common;

namespace PropertyOpsHub.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps creation and modification timestamps from <see cref="TimeProvider"/>, so tests can
/// control time and no handler has to remember to set them.
/// </summary>
/// <remarks>
/// Creation metadata is write-once: on update, <c>CreatedAtUtc</c> is explicitly excluded
/// from the UPDATE so a crafted request cannot rewrite history.
/// </remarks>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;

    public AuditSaveChangesInterceptor(TimeProvider timeProvider) =>
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditableEntity.CreatedAtUtc)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.ModifiedAtUtc)).CurrentValue = null;
                    break;

                case EntityState.Modified:
                    entry.Property(nameof(IAuditableEntity.ModifiedAtUtc)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.CreatedAtUtc)).IsModified = false;
                    break;

                default:
                    break;
            }
        }
    }
}
