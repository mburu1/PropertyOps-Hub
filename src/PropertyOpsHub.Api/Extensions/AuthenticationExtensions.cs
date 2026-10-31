using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PropertyOpsHub.Api.Options;
using PropertyOpsHub.Api.Security;
using PropertyOpsHub.Application.Security;

namespace PropertyOpsHub.Api.Extensions;

/// <summary>JWT bearer authentication and the role-based policies built on it.</summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Registers bearer authentication with validation that rejects anything it is not certain
    /// about: signature, issuer, audience, lifetime and algorithm are all enforced.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.SigningKey.Length >= JwtOptions.MinimumSigningKeyLength,
                $"Jwt:SigningKey must be at least {JwtOptions.MinimumSigningKeyLength} characters.")
            .ValidateOnStart();

        // Read once at startup: a symmetric key has to be known before the first request, and
        // failing here produces a clear message instead of a 401 at runtime.
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException(
                      $"Configuration section '{JwtOptions.SectionName}' is missing.");

        if (jwt.SigningKey.Length < JwtOptions.MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey is missing or shorter than {JwtOptions.MinimumSigningKeyLength} characters. " +
                "Set it with 'dotnet user-secrets' locally, or through the environment in deployment.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep claim names exactly as issued: sub, role, property_id.
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    RequireSignedTokens = true,
                    // Pin the algorithm: without this, a token could claim a weaker one.
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    // Default is five minutes, which lets an expired token work for a while.
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = HttpContextCurrentUser.SubjectClaim,
                    RoleClaimType = HttpContextCurrentUser.RoleClaim
                };

                options.Events = new JwtBearerEvents
                {
                    // Log the reason without the token itself: tokens are bearer credentials.
                    OnAuthenticationFailed = context =>
                    {
                        context.HttpContext.RequestServices
                            .GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                            .CreateLogger("PropertyOpsHub.Api.Authentication")
                            .LogWarning(
                                "Rejected bearer credential for {Path}: {Reason}",
                                context.HttpContext.Request.Path.Value,
                                context.Exception.GetType().Name);

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>Role policies. Add a policy here rather than scattering role strings in controllers.</summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(
                Policies.ManageProperties,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(Roles.GroupFinance, Roles.ItAdmin));

        return services;
    }
}
