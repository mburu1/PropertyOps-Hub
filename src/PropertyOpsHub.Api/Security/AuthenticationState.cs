using System;
using Microsoft.Extensions.Configuration;

namespace PropertyOpsHub.Api.Security;

/// <summary>
/// Whether the API has enough authentication configuration to accept credentials.
/// </summary>
/// <remarks>
/// This is deliberately a value, not an exception: a process that cannot authenticate anybody
/// should still start, answer liveness, report itself unready, and refuse every protected
/// endpoint. A crash loop gives an operator a restart counter; a readiness failure names the
/// setting that is missing.
/// </remarks>
public sealed record AuthenticationState(bool IsConfigured, string? Problem)
{
    public static AuthenticationState From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(Options.JwtOptions.SectionName);

        var issuer = section["Issuer"];
        var audience = section["Audience"];
        var signingKey = section["SigningKey"];

        if (string.IsNullOrWhiteSpace(issuer))
        {
            return new AuthenticationState(false, "Jwt:Issuer is not set.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            return new AuthenticationState(false, "Jwt:Audience is not set.");
        }

        if (string.IsNullOrWhiteSpace(signingKey) ||
            signingKey.Length < Options.JwtOptions.MinimumSigningKeyLength)
        {
            return new AuthenticationState(
                false,
                $"Jwt:SigningKey is missing or shorter than {Options.JwtOptions.MinimumSigningKeyLength} characters.");
        }

        return new AuthenticationState(true, null);
    }
}
