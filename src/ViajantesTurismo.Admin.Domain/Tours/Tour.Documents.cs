using PandaBear.BuildingBlocks;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Domain.Tours;

public sealed partial class Tour
{
    /// <summary>Checks whether a booking on this tour may receive customer-facing documents.</summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <returns>
    /// A success result for a confirmed or completed booking; a not-found failure when the booking does
    /// not belong to this tour; otherwise, a booking-not-accepted conflict.
    /// </returns>
    public Result CanReceiveCustomerDocuments(Guid bookingId)
    {
        var bookingResult = FindBooking(bookingId).ToNotFoundResult(bookingId, "this tour");
        if (bookingResult.IsFailure)
        {
            return bookingResult.ConvertError();
        }

        return bookingResult.Value.Status.IsAccepted() ? Result.Ok() : DocumentErrors.BookingIsNotAccepted();
    }
}
