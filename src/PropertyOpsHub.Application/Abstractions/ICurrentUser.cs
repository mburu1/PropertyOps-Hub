using System;
using System.Collections.Generic;

namespace PropertyOpsHub.Application.Abstractions;

/// <summary>
/// The authenticated caller. Implemented in the API from the validated JWT; the application
/// layer never reads <c>HttpContext</c> directly.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    /// <summary>Role names from the token, for example <c>GroupFinance</c>.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Properties the caller may see. Empty for group-wide roles.</summary>
    IReadOnlyCollection<Guid> PropertyIds { get; }
}
