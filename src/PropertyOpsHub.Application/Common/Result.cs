using System;

namespace PropertyOpsHub.Application.Common;

/// <summary>
/// Outcome of a use case: either success, or an <see cref="Common.Error"/> describing a
/// failure the caller can act on. Expected business failures are values, not exceptions.
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    /// <summary>The failure, or <c>null</c> when the operation succeeded.</summary>
    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(null);

    public static Result<TValue> Success<TValue>(TValue value) => new(value);

    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static Result<TValue> Failure<TValue>(Error error) =>
        new(error ?? throw new ArgumentNullException(nameof(error)));
}

/// <summary>A successful result that carries a value, or a failure that carries an error.</summary>
/// <typeparam name="TValue">Payload type.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue value)
        : base(null) => _value = value;

    internal Result(Error error)
        : base(error) => _value = default;

    /// <summary>The payload. Throws if the result is a failure; check <see cref="Result.IsSuccess"/> first.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error!.Code}).");

    /// <summary>Returns the value or the supplied fallback when the result is a failure.</summary>
    public TValue ValueOrDefault(TValue fallback) => IsSuccess ? _value! : fallback;
}
