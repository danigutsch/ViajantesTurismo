using SharedKernel.Results;

namespace ViajantesTurismo.Admin.ApiService;

internal static class AdminEndpointResults
{
    public static TResult MatchConflictValidationFailure<TResult>(
        Result result,
        Func<TResult> whenConflict,
        Func<TResult> whenInvalid)
    {
        return result.Status switch
        {
            ResultStatus.Conflict => whenConflict(),
            ResultStatus.Invalid => whenInvalid(),
            _ => throw new InvalidOperationException($"Unsupported result status '{result.Status}'."),
        };
    }

    public static TResult MatchNotFoundConflictFailure<TResult>(
        Result result,
        Func<TResult> whenNotFound,
        Func<TResult> whenConflict)
    {
        return result.Status switch
        {
            ResultStatus.NotFound => whenNotFound(),
            ResultStatus.Conflict => whenConflict(),
            _ => throw new InvalidOperationException($"Unsupported result status '{result.Status}'."),
        };
    }

    public static TResult MatchNotFoundConflictValidationFailure<TResult>(
        Result result,
        Func<TResult> whenNotFound,
        Func<TResult> whenConflict,
        Func<TResult> whenInvalid)
    {
        return result.Status switch
        {
            ResultStatus.NotFound => whenNotFound(),
            ResultStatus.Conflict => whenConflict(),
            ResultStatus.Invalid => whenInvalid(),
            _ => throw new InvalidOperationException($"Unsupported result status '{result.Status}'."),
        };
    }

    public static TResult MatchNotFoundValidationFailure<TResult>(
        Result result,
        Func<TResult> whenNotFound,
        Func<TResult> whenInvalid)
    {
        return result.Status switch
        {
            ResultStatus.NotFound => whenNotFound(),
            ResultStatus.Invalid => whenInvalid(),
            _ => throw new InvalidOperationException($"Unsupported result status '{result.Status}'."),
        };
    }
}
