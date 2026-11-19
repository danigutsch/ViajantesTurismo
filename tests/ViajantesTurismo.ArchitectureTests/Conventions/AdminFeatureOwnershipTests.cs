using ViajantesTurismo.ArchitectureTests.Repository;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks ownership of the Admin booking and tour operations.</summary>
public sealed class AdminFeatureOwnershipTests
{
    [Theory]
    [InlineData("src/ViajantesTurismo.Admin.Api/Bookings/CancelBooking/BookingsCancelBookingEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Api/Bookings/CompleteBooking/BookingsCompleteBookingEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Api/Bookings/ConfirmBooking/BookingsConfirmBookingEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Api/Bookings/CreateBooking/BookingsCreateBookingEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Api/Bookings/DeleteBooking/BookingsDeleteBookingEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Api/Bookings/UpdateBookingDetails/BookingsUpdateBookingDetailsEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Api/Bookings/UpdateBookingDiscount/BookingsUpdateBookingDiscountEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Api/Bookings/UpdateBookingNotes/BookingsUpdateBookingNotesEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Api/Tours/CreateTour/ToursCreateTourEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Api/Tours/UpdateTour/ToursUpdateTourEndpoint.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Application/Bookings/BookingMapper.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Infrastructure/Tours/BookingConfiguration.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Infrastructure/Tours/TourConfiguration.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Infrastructure/Tours/TourStore.cs")]
    [InlineData("src/ViajantesTurismo.Admin.Infrastructure/Tours/TourCommandStore.cs")]
    [InlineData("tests/ViajantesTurismo.Admin.UnitTests/Application/Bookings/ConfirmBooking/ConfirmBookingCommandHandlerTests.cs")]
    [InlineData("tests/ViajantesTurismo.Admin.UnitTests/Application/Bookings/CreateBooking/CreateBookingTimeTests.cs")]
    [InlineData("tests/ViajantesTurismo.Admin.UnitTests/Application/Bookings/DeleteBooking/DeleteBookingCommandHandlerTests.cs")]
    [InlineData("tests/ViajantesTurismo.Admin.UnitTests/Application/Bookings/UpdateBookingDetails/UpdateBookingDetailsCommandHandlerTests.cs")]
    [InlineData("tests/ViajantesTurismo.Admin.UnitTests/Application/Tours/CreateTour/CreateTourCommandHandlerTests.cs")]
    [InlineData("tests/ViajantesTurismo.Admin.ApiServiceTests/Tours/TourCurrencyEndpointTests.cs")]
    public void Files_live_with_their_feature(string relativePath)
    {
        // Arrange
        var repositoryRoot = RepositoryRoot.Locate();
        var expectedPath = Path.Combine(repositoryRoot, relativePath);

        // Act
        var exists = File.Exists(expectedPath);

        // Assert
        exists.ShouldBeTrue($"Expected a feature-owned file at {expectedPath}.");
    }
}
