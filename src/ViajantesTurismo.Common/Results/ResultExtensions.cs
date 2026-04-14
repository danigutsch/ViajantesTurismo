namespace ViajantesTurismo.Common.Results;

/// <summary>
/// Extension methods for Result types.
/// </summary>
public static class ResultExtensions
{
    private const string CannotConvertASuccessfulResultMessage = "Cannot convert a successful result. Only failed results can be converted.";
    private const string FailedResultMustContainErrorDetailsMessage = "Failed results must contain error details.";

    /// <summary>
    /// Converts a failed <see cref="Result"/> to <see cref="Result{TTarget}"/>.
    /// </summary>
    /// <typeparam name="TTarget">The target result type.</typeparam>
    /// <param name="source">The source result to convert.</param>
    /// <returns>A Result&lt;TTarget&gt; with the same error information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the source result is successful.</exception>
    public static Result<TTarget> ConvertError<TTarget>(this Result source) where TTarget : notnull
    {
        return ConvertFailureResult(
            source.Status,
            source.ErrorDetails,
            CreateGenericFactories<TTarget>());
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
        return ConvertFailureResult(
            source.Status,
            source.ErrorDetails,
            NonGenericFactories.Instance);
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
        return ConvertFailureResult(
            source.Status,
            source.ErrorDetails,
            CreateGenericFactories<TTarget>());
    }

    private static FailureResultFactories<Result<TTarget>> CreateGenericFactories<TTarget>()
        where TTarget : notnull =>
        new(
            Result<TTarget>.Invalid,
            Result<TTarget>.NotFound,
            Result<TTarget>.Unauthorized,
            Result<TTarget>.Forbidden,
            Result<TTarget>.Error,
            Result<TTarget>.Conflict,
            Result<TTarget>.CriticalError,
            Result<TTarget>.Unavailable);

    private static TResult ConvertFailureResult<TResult>(
        ResultStatus status,
        ResultError? errorDetails,
        FailureResultFactories<TResult> factories)
    {
        if (status is ResultStatus.Ok or ResultStatus.Created or ResultStatus.Accepted or ResultStatus.NoContent)
        {
            throw new InvalidOperationException(CannotConvertASuccessfulResultMessage);
        }

        var details = errorDetails ?? throw new InvalidOperationException(FailedResultMustContainErrorDetailsMessage);

        return status switch
        {
            ResultStatus.Invalid => factories.Invalid(details.Detail, details.ValidationErrors ?? throw new InvalidOperationException(FailedResultMustContainErrorDetailsMessage)),
            ResultStatus.NotFound => factories.NotFound(details.Detail),
            ResultStatus.Unauthorized => factories.Unauthorized(details.Detail),
            ResultStatus.Forbidden => factories.Forbidden(details.Detail),
            ResultStatus.Error => factories.Error(details.Detail),
            ResultStatus.Conflict => factories.Conflict(details.Detail),
            ResultStatus.CriticalError => factories.CriticalError(details.Detail),
            ResultStatus.Unavailable => factories.Unavailable(details.Detail),
            ResultStatus.Unknown => throw new InvalidOperationException($"Unsupported result status: {status}"),
            _ => throw new InvalidOperationException($"Unsupported result status: {status}")
        };
    }

    private readonly record struct FailureResultFactories<TResult>(
        Func<string, Dictionary<string, string[]>, TResult> Invalid,
        Func<string, TResult> NotFound,
        Func<string, TResult> Unauthorized,
        Func<string, TResult> Forbidden,
        Func<string, TResult> Error,
        Func<string, TResult> Conflict,
        Func<string, TResult> CriticalError,
        Func<string, TResult> Unavailable);

    private static class NonGenericFactories
    {
        internal static FailureResultFactories<Result> Instance { get; } =
            new(
                Result.Invalid,
                Result.NotFound,
                Result.Unauthorized,
                Result.Forbidden,
                Result.Error,
                Result.Conflict,
                Result.CriticalError,
                Result.Unavailable);
    }
}
