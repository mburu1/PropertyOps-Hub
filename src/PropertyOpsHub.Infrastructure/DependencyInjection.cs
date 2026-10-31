using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Infrastructure.Persistence;
using PropertyOpsHub.Infrastructure.Persistence.Interceptors;
using PropertyOpsHub.Infrastructure.Persistence.Repositories;

namespace PropertyOpsHub.Infrastructure;

/// <summary>Composition root for the persistence layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the context, its interceptors and the repositories.
    /// </summary>
    /// <param name="connectionString">
    /// SQL Server connection string. Taken as a parameter rather than <c>IConfiguration</c> so
    /// this project does not depend on the configuration stack; the API decides where it comes
    /// from (user secrets, environment, Key Vault).
    /// </param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                // Transient SQL failures are retried; 30s keeps a bad query from pinning a request.
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sql.CommandTimeout(30);
            });

            var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();

            // Soft delete first: it rewrites Deleted to Modified, which the audit interceptor then stamps.
            options.AddInterceptors(
                new SoftDeleteSaveChangesInterceptor(timeProvider),
                new AuditSaveChangesInterceptor(timeProvider));
        });

        services.TryAddScoped<IGroupRepository, GroupRepository>();
        services.TryAddScoped<IPropertyRepository, PropertyRepository>();
        services.TryAddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
