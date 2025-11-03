namespace ViajantesTurismo.Common.Results;

/// <summary>
/// Extension methods for Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a failed <see cref="Result{TSource}"/> to <see cref="Result{TTarget}"/>.
    /// </summary>
    /// <typeparam name="TSource">The source result type.</typeparam>
    /// <typeparam name="TTarget">The target result type.</typeparam>
    /// <param name="source">The source result to convert.</param>
    /// <returns>A Result&lt;TTarget&gt; with the same error information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the source result is successful.</exception>
    public static Result<TTarget> ConvertError<TSource, TTarget>(this Result<TSource> source)
    {
        if (source.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result. Only failed results can be converted.");
        }

        return source.Status switch
        {
            ResultStatus.Ok => throw new InvalidOperationException("Cannot convert a successful result. Only failed results can be converted."),
            ResultStatus.Created => throw new InvalidOperationException("Cannot convert a successful result. Only failed results can be converted."),
            ResultStatus.Accepted => throw new InvalidOperationException("Cannot convert a successful result. Only failed results can be converted."),
            ResultStatus.NoContent => throw new InvalidOperationException("Cannot convert a successful result. Only failed results can be converted."),
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
