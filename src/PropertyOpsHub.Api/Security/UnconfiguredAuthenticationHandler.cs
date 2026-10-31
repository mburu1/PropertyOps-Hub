using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PropertyOpsHub.Api.Security;

/// <summary>
/// Scheme used when authentication is not configured. It never succeeds, so every protected
/// endpoint answers 401 and no request can be authenticated by accident.
/// </summary>
/// <remarks>
/// This exists so an unconfigured deployment fails closed at the request level instead of
/// crashing at startup: the process stays up long enough to report <c>/ready</c> and to log the
/// missing setting.
/// </remarks>
internal sealed class UnconfiguredAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Unconfigured";

    public UnconfiguredAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.Fail("Authentication is not configured on this instance."));
}
