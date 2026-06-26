using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

internal static class TourUpdateGuardTestHelpers
{
    public static void AddBookingToTour(Tour tour)
    {
        var result = BookingTestHelpers.AddSingleCustomerBooking(tour);

        Assert.True(result.IsSuccess, "Failed to add booking to tour for test setup.");
    }
}
