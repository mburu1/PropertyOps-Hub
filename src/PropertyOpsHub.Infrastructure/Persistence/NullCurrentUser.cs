using System;
using System.Collections.Generic;
using PropertyOpsHub.Application.Abstractions;

namespace PropertyOpsHub.Infrastructure.Persistence;

/// <summary>
/// An anonymous caller. Used by the design-time factory and by migrations, where there is no
/// request. Because the tenant filters fail closed, a context built with this user sees no
/// rows: that is intentional, and migrations only need the model, not data.
/// </summary>
internal sealed class NullCurrentUser : ICurrentUser
{
    public static NullCurrentUser Instance { get; } = new();

    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public string? Email => null;

    public IReadOnlyCollection<string> Roles => [];

    public IReadOnlyCollection<Guid> PropertyIds => [];
}
