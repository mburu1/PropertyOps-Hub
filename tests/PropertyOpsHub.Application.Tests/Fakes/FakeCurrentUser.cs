using System;
using System.Collections.Generic;
using PropertyOpsHub.Application.Abstractions;
using SecurityRoles = PropertyOpsHub.Application.Security.Roles;

namespace PropertyOpsHub.Application.Tests.Fakes;

/// <summary>Configurable stand-in for the authenticated caller.</summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; init; } = true;

    public Guid? UserId { get; init; } = Guid.CreateVersion7();

    public string? Email { get; init; } = "tester@demo.local";

    public IReadOnlyCollection<string> Roles { get; init; } = [SecurityRoles.ItAdmin];

    public IReadOnlyCollection<Guid> PropertyIds { get; init; } = [];

    public static FakeCurrentUser InRole(string role) => new() { Roles = [role] };

    public static FakeCurrentUser WithProperties(params Guid[] propertyIds) =>
        new() { Roles = [SecurityRoles.StoreController], PropertyIds = propertyIds };

    public static FakeCurrentUser Anonymous() =>
        new() { IsAuthenticated = false, Roles = [], UserId = null, Email = null };
}
