using System;
using Microsoft.Extensions.Logging;

namespace PropertyOpsHub.Api.Logging;

/// <summary>
/// Source-generated log messages for the API. Kept in one place so event ids stay unique and
/// message templates are reviewable; arguments are not evaluated when the level is disabled.
/// </summary>
internal static partial class ApiLogMessages
{
    public const string AuthenticationCategory = "PropertyOpsHub.Api.Authentication";

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Rejected bearer credential for {Path}: {Reason}")]
    internal static partial void AuthenticationRejected(ILogger logger, string path, string reason);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Error,
        Message = "Unhandled exception for {Method} {Path} (trace {TraceId})")]
    internal static partial void UnhandledException(
        ILogger logger,
        Exception exception,
        string method,
        string path,
        string traceId);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Critical,
        Message = "Authentication is not configured: {Problem} Every protected endpoint answers 401 and /ready reports not_ready.")]
    internal static partial void AuthenticationNotConfigured(ILogger logger, string? problem);
}
