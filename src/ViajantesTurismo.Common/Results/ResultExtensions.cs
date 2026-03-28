namespace ViajantesTurismo.Common.Results;

/// <summary>
/// Extension methods for Result types.
/// </summary>
public static class ResultExtensions
{
    private const string? CannotConvertASuccessfulResultMessage = "Cannot convert a successful result. Only failed results can be converted.";

    /// <summary>
    /// Converts a failed <see cref="Result"/> to <see cref="Result{TTarget}"/>.
    /// </summary>
    /// <typeparam name="TTarget">The target result type.</typeparam>
    /// <param name="source">The source result to convert.</param>
    /// <returns>A Result&lt;TTarget&gt; with the same error information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the source result is successful.</exception>
    public static Result<TTarget> ConvertError<TTarget>(this Result source) where TTarget : notnull
    {
        return source.IsSuccess
            ? throw new InvalidOperationException(CannotConvertASuccessfulResultMessage)
            : source.Status switch
            {
                ResultStatus.Ok => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.Created => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.Accepted => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.NoContent => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.Invalid => Result<TTarget>.Invalid(source.ErrorDetails.Detail, source.ErrorDetails.ValidationErrors!),
                ResultStatus.NotFound => Result<TTarget>.NotFound(source.ErrorDetails.Detail),
                ResultStatus.Unauthorized => Result<TTarget>.Unauthorized(source.ErrorDetails.Detail),
                ResultStatus.Forbidden => Result<TTarget>.Forbidden(source.ErrorDetails.Detail),
                ResultStatus.Error => Result<TTarget>.Error(source.ErrorDetails.Detail),
                ResultStatus.Conflict => Result<TTarget>.Conflict(source.ErrorDetails.Detail),
                ResultStatus.CriticalError => Result<TTarget>.CriticalError(source.ErrorDetails.Detail),
                ResultStatus.Unavailable => Result<TTarget>.Unavailable(source.ErrorDetails.Detail),
                ResultStatus.Unknown => throw new InvalidOperationException($"Unsupported result status: {source.Status}"),
                _ => throw new InvalidOperationException($"Unsupported result status: {source.Status}")
            };
    }

    /// <summary>
    /// Converts a failed <see cref="Result{TSource}"/> to <see cref="Result"/>.
    /// </summary>
    /// <typeparam name="TSource">The source result type.</typeparam>
    /// <param name="source">The source result to convert.</param>
    /// <returns>A Result with the same error information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the source result is successful.</exception>
    public static Result ConvertError<TSource>(this Result<TSource> source) where TSource : notnull
    {
        return source.IsSuccess
            ? throw new InvalidOperationException(CannotConvertASuccessfulResultMessage)
            : source.Status switch
            {
                ResultStatus.Ok => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.Created => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.Accepted => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.NoContent => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.Invalid => Result.Invalid(source.ErrorDetails.Detail, source.ErrorDetails.ValidationErrors!),
                ResultStatus.NotFound => Result.NotFound(source.ErrorDetails.Detail),
                ResultStatus.Unauthorized => Result.Unauthorized(source.ErrorDetails.Detail),
                ResultStatus.Forbidden => Result.Forbidden(source.ErrorDetails.Detail),
                ResultStatus.Error => Result.Error(source.ErrorDetails.Detail),
                ResultStatus.Conflict => Result.Conflict(source.ErrorDetails.Detail),
                ResultStatus.CriticalError => Result.CriticalError(source.ErrorDetails.Detail),
                ResultStatus.Unavailable => Result.Unavailable(source.ErrorDetails.Detail),
                ResultStatus.Unknown => throw new InvalidOperationException($"Unsupported result status: {source.Status}"),
                _ => throw new InvalidOperationException($"Unsupported result status: {source.Status}")
            };
    }

    /// <summary>
    /// Converts a failed <see cref="Result{TSource}"/> to <see cref="Result{TTarget}"/>.
    /// </summary>
    /// <typeparam name="TSource">The source result type.</typeparam>
    /// <typeparam name="TTarget">The target result type.</typeparam>
    /// <param name="source">The source result to convert.</param>
    /// <returns>A Result&lt;TTarget&gt; with the same error information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the source result is successful.</exception>
    public static Result<TTarget> ConvertError<TSource, TTarget>(this Result<TSource> source)
        where TSource : notnull
        where TTarget : notnull
    {
        return source.IsSuccess
            ? throw new InvalidOperationException(CannotConvertASuccessfulResultMessage)
            : source.Status switch
            {
                ResultStatus.Ok => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.Created => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.Accepted => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.NoContent => throw new InvalidOperationException(CannotConvertASuccessfulResultMessage),
                ResultStatus.Invalid => Result<TTarget>.Invalid(source.ErrorDetails.Detail, source.ErrorDetails.ValidationErrors!),
                ResultStatus.NotFound => Result<TTarget>.NotFound(source.ErrorDetails.Detail),
                ResultStatus.Unauthorized => Result<TTarget>.Unauthorized(source.ErrorDetails.Detail),
                ResultStatus.Forbidden => Result<TTarget>.Forbidden(source.ErrorDetails.Detail),
                ResultStatus.Error => Result<TTarget>.Error(source.ErrorDetails.Detail),
                ResultStatus.Conflict => Result<TTarget>.Conflict(source.ErrorDetails.Detail),
                ResultStatus.CriticalError => Result<TTarget>.CriticalError(source.ErrorDetails.Detail),
                ResultStatus.Unavailable => Result<TTarget>.Unavailable(source.ErrorDetails.Detail),
                ResultStatus.Unknown => throw new InvalidOperationException($"Unsupported result status: {source.Status}"),
                _ => throw new InvalidOperationException($"Unsupported result status: {source.Status}")
            };
    }
}
