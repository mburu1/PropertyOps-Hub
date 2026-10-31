using System;
using System.ComponentModel.DataAnnotations;

namespace PropertyOpsHub.Api.Options;

/// <summary>
/// Configuration for the access tokens this API issues and accepts.
/// </summary>
/// <remarks>
/// There is deliberately no default signing key. A committed key is a leaked key, so the
/// value comes from user secrets in development and from the environment or a secret store
/// in production, and startup fails if it is missing or too short.
/// </remarks>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum length for an HMAC-SHA256 key, in characters.</summary>
    public const int MinimumSigningKeyLength = 32;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 43200)]
    public int RefreshTokenMinutes { get; set; } = 10080;
}
