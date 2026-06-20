using SharedKernel.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.ApiService;

internal static class AdminEndpointResults
{
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

    public static async Task<TResult> GetPaymentResponse<TResult>(
        Guid bookingId,
        Guid paymentId,
        IQueryService queryService,
        Func<GetPaymentDto, TResult> whenFound,
        Func<Guid, TResult> whenBookingNotFound,
        Func<Guid, TResult> whenPaymentNotFound,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(whenFound);
        ArgumentNullException.ThrowIfNull(whenBookingNotFound);
        ArgumentNullException.ThrowIfNull(whenPaymentNotFound);

        var booking = await queryService.GetBookingById(bookingId, ct);
        if (booking is null)
        {
            return whenBookingNotFound(bookingId);
        }

        var payment = booking.Payments.FirstOrDefault(candidate => candidate.Id == paymentId);

        return payment is null
            ? whenPaymentNotFound(paymentId)
            : whenFound(payment);
    }

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
