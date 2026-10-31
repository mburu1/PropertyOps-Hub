using Microsoft.Extensions.Logging;

namespace PropertyOpsHub.Application.Behaviors;

/// <summary>
/// Source-generated log messages. Compiled delegates avoid boxing and argument-evaluation
/// cost when the level is disabled, and keep message templates in one reviewable place.
/// </summary>
internal static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Handled {RequestName} in {ElapsedMilliseconds:0.0} ms")]
    internal static partial void RequestHandled(ILogger logger, string requestName, double elapsedMilliseconds);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Rejected {RequestName}: {FieldCount} field(s) failed validation")]
    internal static partial void RequestRejectedByValidation(ILogger logger, string requestName, int fieldCount);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Domain rule {RuleCode} violated in {RequestName}")]
    internal static partial void DomainRuleViolated(ILogger logger, string requestName, string ruleCode);
}
