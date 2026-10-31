using System;
using System.Collections.Generic;
using System.Linq;
using PropertyOpsHub.Application.Abstractions;

namespace PropertyOpsHub.Application.Security;

/// <summary>
/// Role names used in tokens and policies. Kept as constants so a typo is a compile error
/// rather than an authorization hole.
/// </summary>
public static class Roles
{
    /// <summary>Sees every property: journals, mappings, consolidated reporting.</summary>
    public const string GroupFinance = "GroupFinance";

    /// <summary>Own property only: dashboards, variance, approvals.</summary>
    public const string PropertyManager = "PropertyManager";

    /// <summary>Own property only: material control, counts, goods receipts.</summary>
    public const string StoreController = "StoreController";

    /// <summary>Interfaces, incidents, access reviews, user management.</summary>
    public const string ItAdmin = "ItAdmin";

    public static IReadOnlyList<string> All { get; } =
    [
        GroupFinance,
        PropertyManager,
        StoreController,
        ItAdmin
    ];
}

/// <summary>Authorization decisions that are shared across use cases.</summary>
public static class CurrentUserExtensions
{
    /// <summary>Case-insensitive role check; tokens are not guaranteed to be normalised.</summary>
    public static bool IsInRole(this ICurrentUser user, string role)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Group-wide roles see every property; everyone else is limited to their claims.</summary>
    public static bool CanSeeAllProperties(this ICurrentUser user) =>
        user.IsInRole(Roles.GroupFinance) || user.IsInRole(Roles.ItAdmin);

    /// <summary>
    /// True when the caller may read the property. Callers that fail this check are given
    /// "not found" rather than "forbidden" so property ids cannot be enumerated.
    /// </summary>
    public static bool CanAccessProperty(this ICurrentUser user, Guid propertyId)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.CanSeeAllProperties() || user.PropertyIds.Contains(propertyId);
    }

    /// <summary>Only finance and IT administration may create or retire properties.</summary>
    public static bool CanManageProperties(this ICurrentUser user) =>
        user.IsInRole(Roles.GroupFinance) || user.IsInRole(Roles.ItAdmin);
}
