using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

internal static class TourUpdateGuardTestHelpers
{
    public static void AddBookingToTour(Tour tour)
    {
        var result = BookingTestHelpers.AddSingleCustomerBooking(tour);

        TestAssert.True(result.IsSuccess, "Failed to add booking to tour for test setup.");
    }
}
