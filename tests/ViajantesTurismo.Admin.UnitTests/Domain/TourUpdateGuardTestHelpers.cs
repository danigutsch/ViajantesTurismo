using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

internal static class TourUpdateGuardTestHelpers
{
    public static void AddBookingToTour(Tour tour)
    {
        var result = BookingTestHelpers.AddSingleCustomerBooking(tour);

        (result.IsSuccess).ShouldBeTrue("Failed to add booking to tour for test setup.");
    }
}
