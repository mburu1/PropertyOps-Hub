using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PropertyOpsHub.Domain.Common;
using AppValidationException = PropertyOpsHub.Application.Common.Exceptions.ValidationException;

namespace PropertyOpsHub.Application.Behaviors;

/// <summary>
/// Logs one line per use case with its outcome and duration.
/// </summary>
/// <remarks>
/// Deliberately logs only the request <em>type</em>, never its payload: commands carry
/// personal data and money, and structured logs leave the building.
/// </remarks>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var response = await next(cancellationToken);

            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            LogMessages.RequestHandled(_logger, requestName, elapsedMilliseconds);

            return response;
        }
        catch (AppValidationException exception)
        {
            LogMessages.RequestRejectedByValidation(_logger, requestName, exception.Errors.Count);
            throw;
        }
        catch (DomainException exception)
        {
            LogMessages.DomainRuleViolated(_logger, requestName, exception.Code);
            throw;
        }
    }
}
