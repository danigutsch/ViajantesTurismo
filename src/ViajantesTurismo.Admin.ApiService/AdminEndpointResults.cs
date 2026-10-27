using PandaBear.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>Maps application results and booking lookups to endpoint responses.</summary>
internal static class AdminEndpointResults
{
    /// <summary>On success reloads the booking; on failure maps not-found and conflict statuses.</summary>
    public static Task<TResult> MatchNotFoundConflictBookingResponse<TResult>(
        Result result,
        Guid bookingId,
        IQueryService queryService,
        Func<GetBookingDto, TResult> whenFound,
        Func<TResult> whenNotFound,
        Func<TResult> whenConflict,
        CancellationToken ct)
    {
        return result.IsFailure
            ? Task.FromResult(MatchNotFoundConflictFailure(result, whenNotFound, whenConflict))
            : GetBookingResponse(bookingId, queryService, whenFound, _ => whenNotFound(), ct);
    }

    /// <summary>On success reloads the booking; on failure maps not-found, conflict and invalid statuses.</summary>
    public static Task<TResult> MatchNotFoundConflictValidationBookingResponse<TResult>(
        Result result,
        Guid bookingId,
        IQueryService queryService,
        Func<GetBookingDto, TResult> whenFound,
        Func<TResult> whenNotFound,
        Func<TResult> whenConflict,
        Func<TResult> whenInvalid,
        CancellationToken ct)
    {
        return result.IsFailure
            ? Task.FromResult(MatchNotFoundConflictValidationFailure(result, whenNotFound, whenConflict, whenInvalid))
            : GetBookingResponse(bookingId, queryService, whenFound, _ => whenNotFound(), ct);
    }

    /// <summary>On success reloads the booking; on failure maps not-found and invalid statuses.</summary>
    public static Task<TResult> MatchNotFoundValidationBookingResponse<TResult>(
        Result result,
        Guid bookingId,
        IQueryService queryService,
        Func<GetBookingDto, TResult> whenFound,
        Func<TResult> whenNotFound,
        Func<TResult> whenInvalid,
        CancellationToken ct)
    {
        return result.IsFailure
            ? Task.FromResult(MatchNotFoundValidationFailure(result, whenNotFound, whenInvalid))
            : GetBookingResponse(bookingId, queryService, whenFound, _ => whenNotFound(), ct);
    }

    /// <summary>Loads a booking by identifier and maps it to the found or not-found response.</summary>
    public static async Task<TResult> GetBookingResponse<TResult>(
        Guid bookingId,
        IQueryService queryService,
        Func<GetBookingDto, TResult> whenFound,
        Func<Guid, TResult> whenNotFound,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(whenFound);
        ArgumentNullException.ThrowIfNull(whenNotFound);

        var booking = await queryService.GetBookingById(bookingId, ct);

        return booking is null
            ? whenNotFound(bookingId)
            : whenFound(booking);
    }

    /// <summary>Maps a failed result with conflict or invalid status; other statuses throw.</summary>
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

    /// <summary>Maps a failed result with not-found or conflict status; other statuses throw.</summary>
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

    /// <summary>Maps a failed result with not-found, conflict or invalid status; other statuses throw.</summary>
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

    /// <summary>Maps a failed result with not-found or invalid status; other statuses throw.</summary>
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
