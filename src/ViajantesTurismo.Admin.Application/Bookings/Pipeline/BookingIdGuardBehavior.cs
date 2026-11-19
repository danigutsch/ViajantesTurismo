using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Application.Bookings.CompleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDiscount;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingNotes;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Bookings.Pipeline;

/// <summary>
/// Returns the booking-not-found result before transaction or store access when a command targets
/// an existing booking with <see cref="Guid.Empty"/>.
/// </summary>
/// <typeparam name="TRequest">The command request type.</typeparam>
/// <typeparam name="TResponse">The command result type.</typeparam>
[PipelineOrder(PipelineStage.Validation, Order = 1)]
public sealed class BookingIdGuardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : struct, IResultLike<TResponse>
{
    /// <inheritdoc />
    public ValueTask<TResponse> Handle(TRequest request, RequestHandlerContinuation<TResponse> next, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        Guid? bookingId = request switch
        {
            CancelBookingCommand command => command.BookingId,
            CompleteBookingCommand command => command.BookingId,
            ConfirmBookingCommand command => command.BookingId,
            DeleteBookingCommand command => command.BookingId,
            UpdateBookingDetailsCommand command => command.BookingId,
            UpdateBookingDiscountCommand command => command.BookingId,
            UpdateBookingNotesCommand command => command.BookingId,
            _ => null,
        };

        if (bookingId != Guid.Empty)
        {
            return next();
        }

        var notFound = BookingErrors.BookingNotFound().ErrorDetails;
        ArgumentNullException.ThrowIfNull(notFound);

        return ValueTask.FromResult(TResponse.FromError(notFound));
    }
}
