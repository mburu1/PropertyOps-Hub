using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using AppValidationException = PropertyOpsHub.Application.Common.Exceptions.ValidationException;

namespace PropertyOpsHub.Application.Behaviors;

/// <summary>
/// Runs every registered <see cref="IValidator{T}"/> for the request before the handler.
/// Invalid input never reaches a handler, so handlers can trust their command object and
/// only deal with business outcomes.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IReadOnlyList<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) =>
        _validators = validators as IReadOnlyList<IValidator<TRequest>> ?? validators.ToList();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Count == 0)
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        // Failures only: warnings and informational severities do not block the request.
        var failures = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null && failure.Severity == Severity.Error)
            .Select(failure => (failure.PropertyName, failure.ErrorMessage))
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        throw AppValidationException.FromFailures(failures);
    }
}
