using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PropertyOpsHub.Application.Behaviors;

namespace PropertyOpsHub.Application;

/// <summary>Composition root for the application layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers request handlers, validators and the pipeline. Order matters: the logging
    /// behaviour is outermost, so a rejected request is still logged with its duration.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
