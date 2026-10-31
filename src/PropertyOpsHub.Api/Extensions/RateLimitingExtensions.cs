using System;
using System.Globalization;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyOpsHub.Api.Mapping;
using PropertyOpsHub.Api.Security;

namespace PropertyOpsHub.Api.Extensions;

/// <summary>Named rate-limit policies.</summary>
public static class RateLimitPolicies
{
    /// <summary>Default for reads.</summary>
    public const string Reads = "reads";

    /// <summary>Stricter limit for state changes.</summary>
    public const string Writes = "writes";
}

/// <summary>
/// Request throttling, partitioned by authenticated subject when available and by client IP
/// otherwise. Authentication runs before the limiter, so a caller cannot dodge their bucket by
/// sending an unauthenticated flood first.
/// </summary>
public static class RateLimitingExtensions
{
    private const int ReadsPerMinute = 120;
    private const int WritesPerMinute = 30;

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.Reads, httpContext =>
                Partition(httpContext, ReadsPerMinute));

            options.AddPolicy(RateLimitPolicies.Writes, httpContext =>
                Partition(httpContext, WritesPerMinute));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfterSeconds = 0;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                    context.HttpContext.Response.Headers.RetryAfter =
                        retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                }

                var problem = ErrorMapping.TooManyRequests(
                    "Too many requests. Wait and try again.",
                    retryAfterSeconds);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response
                    .WriteAsJsonAsync(problem, cancellationToken)
                    .ConfigureAwait(false);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> Partition(HttpContext httpContext, int permitsPerMinute)
    {
        var key = ResolvePartitionKey(httpContext);

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }

    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        var subject = httpContext.User?.FindFirst(Security.HttpContextCurrentUser.SubjectClaim)?.Value;

        if (!string.IsNullOrWhiteSpace(subject))
        {
            return $"sub:{subject}";
        }

        var address = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return $"ip:{address}";
    }
}
