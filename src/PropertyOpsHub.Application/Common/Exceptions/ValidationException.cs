using System;
using System.Collections.Generic;
using System.Linq;

namespace PropertyOpsHub.Application.Common.Exceptions;

/// <summary>
/// Thrown by the validation pipeline behaviour when a request fails FluentValidation rules.
/// Carries per-field messages; the API maps it to a 400 ProblemDetails response.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    /// <summary>Field name to messages. Never contains values the caller did not supply.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Builds the dictionary from FluentValidation failures, grouping repeated fields and
    /// dropping duplicates while preserving the order rules ran in.
    /// </summary>
    public static ValidationException FromFailures(IEnumerable<(string PropertyName, string ErrorMessage)> failures) =>
        new(failures
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal));
}
