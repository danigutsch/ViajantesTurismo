using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Used instead of exceptions for business rule validation.
/// </summary>
public readonly struct Result : IEquatable<Result>
{
    private readonly ResultError? _error;

    internal Result(ResultStatus status, ResultError? error)
    {
        Status = status;
        _error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(false, nameof(ErrorDetails))]
    public bool IsSuccess => Status is ResultStatus.Ok or ResultStatus.NoContent or ResultStatus.Accepted;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ErrorDetails))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the status of the result.
    /// </summary>
    public ResultStatus Status { get; }

    /// <summary>
    /// Gets the error information if the operation failed, otherwise null.
    /// </summary>
    public ResultError? ErrorDetails => IsFailure ? _error : null;

    /// <summary>
    /// Creates a successful result with Ok status.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Ok() => new(ResultStatus.Ok, null);

    /// <summary>
    /// Creates a successful result with NoContent status.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result NoContent() => new(ResultStatus.NoContent, null);

    /// <summary>
    /// Creates a successful result with Accepted status.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Accepted() => new(ResultStatus.Accepted, null);

    /// <summary>
    /// Creates a failed result with Invalid status.
    /// </summary>
    /// <param name="detail">The error detail describing why the operation failed.</param>
    /// <param name="validationErrors">Optional validation errors.</param>
    /// <returns>A failed result.</returns>
    public static Result Invalid(string detail, Dictionary<string, string[]>? validationErrors = null)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail));
        }

        if (validationErrors?.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(validationErrors), "Validation errors dictionary cannot be empty.");
        }

        return new Result(ResultStatus.Invalid, new ResultError(detail, validationErrors));
    }

    /// <summary>
    /// Creates a failed result with NotFound status.
    /// </summary>
    /// <param name="detail">The error detail describing what was not found.</param>
    /// <returns>A failed result.</returns>
    public static Result NotFound(string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result(ResultStatus.NotFound, new ResultError(detail));

    /// <summary>
    /// Creates a failed result with Unauthorized status.
    /// </summary>
    /// <param name="detail">The error detail describing the authorization failure.</param>
    /// <returns>A failed result.</returns>
    public static Result Unauthorized(string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result(ResultStatus.Unauthorized, new ResultError(detail));

    /// <summary>
    /// Creates a failed result with Forbidden status.
    /// </summary>
    /// <param name="detail">The error detail describing why access is forbidden.</param>
    /// <returns>A failed result.</returns>
    public static Result Forbidden(string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result(ResultStatus.Forbidden, new ResultError(detail));

    /// <summary>
    /// Creates a failed result with Conflict status.
    /// </summary>
    /// <param name="detail">The error detail describing the conflict.</param>
    /// <returns>A failed result.</returns>
    public static Result Conflict(string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result(ResultStatus.Conflict, new ResultError(detail));

    /// <summary>
    /// Creates a failed result with Error status.
    /// </summary>
    /// <param name="detail">The error detail describing what went wrong.</param>
    /// <returns>A failed result.</returns>
    public static Result Error(string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result(ResultStatus.Error, new ResultError(detail));

    /// <summary>
    /// Creates a failed result with CriticalError status.
    /// </summary>
    /// <param name="detail">The error detail describing the critical error.</param>
    /// <returns>A failed result.</returns>
    public static Result CriticalError(string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result(ResultStatus.CriticalError, new ResultError(detail));

    /// <summary>
    /// Creates a failed result with Unavailable status.
    /// </summary>
    /// <param name="detail">The error detail describing why the resource is unavailable.</param>
    /// <returns>A failed result.</returns>
    public static Result Unavailable(string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result(ResultStatus.Unavailable, new ResultError(detail));

    /// <summary>
    /// Determines whether two Result instances are equal.
    /// </summary>
    public static bool operator ==(Result left, Result right) => left.Equals(right);

    /// <summary>
    /// Determines whether two Result instances are not equal.
    /// </summary>
    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    /// <summary>
    /// Determines whether the specified object is equal to the current Result.
    /// </summary>
    public override bool Equals(object? obj) => obj is Result result && Equals(result);

    /// <summary>
    /// Determines whether the specified Result is equal to the current Result.
    /// </summary>
    public bool Equals(Result other) =>
        IsSuccess == other.IsSuccess &&
        Status == other.Status &&
        _error == other._error;

    /// <summary>
    /// Returns the hash code for this Result.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(IsSuccess, Status, _error);

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString() =>
        IsSuccess
            ? $"Success: {Status}"
            : $"Failure: {Status} - {_error?.Detail}";
}

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail with an error.
/// Used instead of exceptions for business rule validation.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
#pragma warning disable CA1000 // Do not declare static members on generic types - this is the Result pattern
public readonly struct Result<T> : IEquatable<Result<T>>
#pragma warning restore CA1000
{
    private readonly T? _value;
    private readonly ResultError? _error;

    private Result(ResultStatus status, T? value, ResultError? error)
    {
        Status = status;
        _value = value;
        _error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(ErrorDetails))]
    public bool IsSuccess => Status is ResultStatus.Ok or ResultStatus.Created or ResultStatus.Accepted;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ErrorDetails))]
    [MemberNotNullWhen(false, nameof(Value))]
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the status of the result.
    /// </summary>
    public ResultStatus Status { get; }

    /// <summary>
    /// Gets the value if the operation succeeded.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessing Value on a failed result.</exception>
    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException($"Cannot access Value of a failed result. Error: {_error?.Detail}");
            }

            return _value;
        }
    }

    /// <summary>
    /// Gets the error information if the operation failed, otherwise null.
    /// </summary>
    public ResultError? ErrorDetails => IsFailure ? _error : null;

    /// <summary>
    /// Creates a successful result with a value and Ok status.
    /// </summary>
    /// <param name="value">The value returned by the successful operation.</param>
    /// <returns>A successful result containing the value.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> Ok(T value)
#pragma warning restore CA1000
    {
        return value is null
            ? throw new ArgumentNullException(nameof(value), "Success value cannot be null.")
            : new Result<T>(ResultStatus.Ok, value, null);
    }

    /// <summary>
    /// Creates a successful result with a value and Created status.
    /// </summary>
    /// <param name="value">The value returned by the successful operation.</param>
    /// <returns>A successful result containing the value.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> Created(T value)
#pragma warning restore CA1000
    {
        return value is null
            ? throw new ArgumentNullException(nameof(value), "Success value cannot be null.")
            : new Result<T>(ResultStatus.Created, value, null);
    }

    /// <summary>
    /// Creates a successful result with a value and Accepted status.
    /// </summary>
    /// <param name="value">The value returned by the successful operation.</param>
    /// <returns>A successful result containing the value.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> Accepted(T value)
#pragma warning restore CA1000
    {
        return value is null
            ? throw new ArgumentNullException(nameof(value), "Success value cannot be null.")
            : new Result<T>(ResultStatus.Accepted, value, null);
    }

    /// <summary>
    /// Creates a failed result with Invalid status.
    /// </summary>
    /// <param name="detail">The error detail describing why the operation failed.</param>
    /// <param name="validationErrors">Optional validation errors.</param>
    /// <returns>A failed result.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> Invalid(string detail, Dictionary<string, string[]>? validationErrors = null)
#pragma warning restore CA1000
    {
        return string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result<T>(ResultStatus.Invalid, default, new ResultError(detail, validationErrors));
    }

    /// <summary>
    /// Creates a failed result with NotFound status.
    /// </summary>
    /// <param name="detail">The error detail describing what was not found.</param>
    /// <returns>A failed result.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> NotFound(string detail)
#pragma warning restore CA1000
    {
        return string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result<T>(ResultStatus.NotFound, default, new ResultError(detail));
    }

    /// <summary>
    /// Creates a failed result with Unauthorized status.
    /// </summary>
    /// <param name="detail">The error detail describing the authorization failure.</param>
    /// <returns>A failed result.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> Unauthorized(string detail)
#pragma warning restore CA1000
    {
        return string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result<T>(ResultStatus.Unauthorized, default, new ResultError(detail));
    }

    /// <summary>
    /// Creates a failed result with Forbidden status.
    /// </summary>
    /// <param name="detail">The error detail describing why access is forbidden.</param>
    /// <returns>A failed result.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> Forbidden(string detail)
#pragma warning restore CA1000
    {
        return string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result<T>(ResultStatus.Forbidden, default, new ResultError(detail));
    }

    /// <summary>
    /// Creates a failed result with Conflict status.
    /// </summary>
    /// <param name="detail">The error detail describing the conflict.</param>
    /// <returns>A failed result.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> Conflict(string detail)
#pragma warning restore CA1000
    {
        return string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result<T>(ResultStatus.Conflict, default, new ResultError(detail));
    }

    /// <summary>
    /// Creates a failed result with Error status.
    /// </summary>
    /// <param name="detail">The error detail describing what went wrong.</param>
    /// <returns>A failed result.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> Error(string detail)
#pragma warning restore CA1000
    {
        return string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result<T>(ResultStatus.Error, default, new ResultError(detail));
    }

    /// <summary>
    /// Creates a failed result with CriticalError status.
    /// </summary>
    /// <param name="detail">The error detail describing the critical error.</param>
    /// <returns>A failed result.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> CriticalError(string detail)
#pragma warning restore CA1000
    {
        return string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result<T>(ResultStatus.CriticalError, default, new ResultError(detail));
    }

    /// <summary>
    /// Creates a failed result with Unavailable status.
    /// </summary>
    /// <param name="detail">The error detail describing why the resource is unavailable.</param>
    /// <returns>A failed result.</returns>
#pragma warning disable CA1000 // Result pattern requires static factory methods
    public static Result<T> Unavailable(string detail)
#pragma warning restore CA1000
    {
        return string.IsNullOrWhiteSpace(detail)
            ? throw new ArgumentException("Error detail cannot be null or whitespace.", nameof(detail))
            : new Result<T>(ResultStatus.Unavailable, default, new ResultError(detail));
    }

    /// <summary>
    /// Implicitly converts a successful value to a Result&lt;T&gt;.
    /// </summary>
    /// <param name="value">The success value.</param>
    public static implicit operator Result<T>(T value) => Ok(value);

    /// <summary>
    /// Determines whether two Result instances are equal.
    /// </summary>
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two Result instances are not equal.
    /// </summary>
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    /// <summary>
    /// Converts Result&lt;T&gt; to Result (discarding the value).
    /// </summary>
    public Result ToResult() => IsSuccess
        ? new Result(Status, null)
        : new Result(Status, _error);

    /// <summary>
    /// Determines whether the specified object is equal to the current Result.
    /// </summary>
    public override bool Equals(object? obj) => obj is Result<T> result && Equals(result);

    /// <summary>
    /// Determines whether the specified Result is equal to the current Result.
    /// </summary>
    public bool Equals(Result<T> other)
    {
        if (IsSuccess != other.IsSuccess)
        {
            return false;
        }

        if (Status != other.Status)
        {
            return false;
        }

        if (IsSuccess)
        {
            return EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        return _error == other._error;
    }

    /// <summary>
    /// Returns the hash code for this Result.
    /// </summary>
    public override int GetHashCode() =>
        IsSuccess
            ? HashCode.Combine(IsSuccess, Status, _value)
            : HashCode.Combine(IsSuccess, Status, _error);

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString() =>
        IsSuccess
            ? $"Success: {Status} - {_value}"
            : $"Failure: {Status} - {_error?.Detail}";
}
