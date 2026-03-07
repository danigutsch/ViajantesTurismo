using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourUpdateGuardTests
{
    private static Tour CreateTour(
        string identifier = "TEST2024",
        decimal basePrice = 2000.00m,
        Currency? currency = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(1);
        var end = endDate ?? start.AddDays(7);

        return Tour.Create(
            identifier: identifier,
            name: "Test Tour",
            startDate: start,
            endDate: end,
            basePrice: basePrice,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: currency ?? Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    private static void AddBookingToTour(Tour tour)
    {
        var result = tour.AddBooking(
            Guid.CreateVersion7(),
            BikeType.Regular,
            null,
            null,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            0m,
            null,
            null);

        Assert.True(result.IsSuccess, "Failed to add booking to tour for test setup.");
    }

    [Fact]
    public void UpdateDetails_without_bookings_should_succeed()
    {
        // Arrange
        var tour = CreateTour();

        // Act
        var result = tour.UpdateDetails("NEWID", "New Name");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("NEWID", tour.Identifier);
        Assert.Equal("New Name", tour.Name);
    }

    [Fact]
    public void UpdateDetails_with_bookings_should_fail()
    {
        // Arrange
        var tour = CreateTour(identifier: "ORIG2024");
        AddBookingToTour(tour);

        // Act — change the identifier
        var result = tour.UpdateDetails("NEWID", "New Name");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be changed if bookings exist", result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateDetails_with_bookings_same_identifier_should_succeed()
    {
        // Arrange
        var tour = CreateTour(identifier: "KEEP2024");
        AddBookingToTour(tour);

        // Act — keep the same identifier, change only the name
        var result = tour.UpdateDetails("KEEP2024", "Updated Name");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("KEEP2024", tour.Identifier);
        Assert.Equal("Updated Name", tour.Name);
    }

    [Fact]
    public void UpdateCurrency_without_bookings_should_succeed()
    {
        // Arrange
        var tour = CreateTour(currency: Currency.UsDollar);

        // Act
        var result = tour.UpdateCurrency(Currency.Euro);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Currency.Euro, tour.Pricing.Currency);
    }

    [Fact]
    public void UpdateCurrency_with_bookings_should_fail()
    {
        // Arrange
        var tour = CreateTour(currency: Currency.UsDollar);
        AddBookingToTour(tour);

        // Act
        var result = tour.UpdateCurrency(Currency.Euro);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be changed if bookings exist", result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }
}
