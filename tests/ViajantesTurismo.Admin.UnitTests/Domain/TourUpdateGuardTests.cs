using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourUpdateGuardTests
{
    private static void AddBookingToTour(Tour tour)
    {
        var result = BookingTestHelpers.AddSingleCustomerBooking(tour);

        Assert.True(result.IsSuccess, "Failed to add booking to tour for test setup.");
    }

    [Fact]
    public void Update_Details_Without_Bookings_Should_Succeed()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();

        // Act
        var result = tour.UpdateDetails("NEWID", "New Name");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("NEWID", tour.Identifier);
        Assert.Equal("New Name", tour.Name);
    }

    [Fact]
    public void Update_Details_With_Bookings_Should_Fail()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "ORIG2024"));
        AddBookingToTour(tour);

        // Act — change the identifier
        var result = tour.UpdateDetails("NEWID", "New Name");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be changed if bookings exist", result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Update_Details_With_Bookings_Same_Identifier_Should_Succeed()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "KEEP2024"));
        AddBookingToTour(tour);

        // Act — keep the same identifier, change only the name
        var result = tour.UpdateDetails("KEEP2024", "Updated Name");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("KEEP2024", tour.Identifier);
        Assert.Equal("Updated Name", tour.Name);
    }

    [Fact]
    public void Update_Currency_Without_Bookings_Should_Succeed()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.UsDollar)));

        // Act
        var result = tour.UpdateCurrency(Currency.Euro);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Currency.Euro, tour.Pricing.Currency);
    }

    [Fact]
    public void Update_Currency_With_Bookings_Should_Fail()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.UsDollar)));
        AddBookingToTour(tour);

        // Act
        var result = tour.UpdateCurrency(Currency.Euro);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be changed if bookings exist", result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }
}
