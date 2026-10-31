using System;

namespace PropertyOpsHub.Domain.Common;

/// <summary>
/// An entity whose creation and last modification timestamps are maintained by the
/// persistence layer, never by callers.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; }

    DateTimeOffset? ModifiedAtUtc { get; }
}

/// <summary>
/// An entity that is never physically deleted: rows are flagged and filtered out by the
/// persistence layer, so historical journals keep pointing at something.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
}
