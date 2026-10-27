using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Write-side tour setup for document command handler tests.</summary>
/// <param name="TourStore">Store holding the tour.</param>
/// <param name="TourId">Id of the tour.</param>
/// <param name="BookingId">Id of the single booking on the tour.</param>
internal sealed record DocumentTourTestData(FakeTourStore TourStore, Guid TourId, Guid BookingId)
{
    /// <summary>Creates a tour store holding one tour with a single booking in the given status.</summary>
    public static DocumentTourTestData WithBooking(BookingStatus status)
    {
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        if (status is BookingStatus.Confirmed or BookingStatus.Completed)
        {
            tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue("Failed to confirm the test booking.");
        }

        if (status is BookingStatus.Completed)
        {
            tour.CompleteBooking(booking.Id).IsSuccess.ShouldBeTrue("Failed to complete the test booking.");
        }

        if (status is BookingStatus.Cancelled)
        {
            tour.CancelBooking(booking.Id).IsSuccess.ShouldBeTrue("Failed to cancel the test booking.");
        }

        var store = new FakeTourStore();
        store.Add(tour);
        return new DocumentTourTestData(store, tour.Id, booking.Id);
    }
}
