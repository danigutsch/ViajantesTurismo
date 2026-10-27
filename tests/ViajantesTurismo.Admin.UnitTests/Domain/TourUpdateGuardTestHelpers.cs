using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Helpers for tour update guard tests.</summary>
internal static class TourUpdateGuardTestHelpers
{
    /// <summary>Adds a single-customer booking to the tour, failing setup if the add is rejected.</summary>
    public static void AddBookingToTour(Tour tour)
    {
        var result = BookingTestHelpers.AddSingleCustomerBooking(tour);

        result.IsSuccess.ShouldBeTrue("Failed to add booking to tour for test setup.");
    }
}
