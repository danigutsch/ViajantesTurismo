using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourUpdateGuardTests
{
    [Fact]
    public void Update_details_without_bookings_should_succeed()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();

        // Act
        var result = tour.UpdateDetails("NEWID", "New Name");

        // Assert
        TestAssert.True(result.IsSuccess);
        TestAssert.Equal("NEWID", tour.Identifier);
        TestAssert.Equal("New Name", tour.Name);
    }

    [Fact]
    public void Update_details_with_bookings_should_fail()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "ORIG2024"));
        TourUpdateGuardTestHelpers.AddBookingToTour(tour);

        // Act — change the identifier
        var result = tour.UpdateDetails("NEWID", "New Name");

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.Contains("cannot be changed if bookings exist", result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Update_details_with_bookings_same_identifier_should_succeed()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "KEEP2024"));
        TourUpdateGuardTestHelpers.AddBookingToTour(tour);

        // Act — keep the same identifier, change only the name
        var result = tour.UpdateDetails("KEEP2024", "Updated Name");

        // Assert
        TestAssert.True(result.IsSuccess);
        TestAssert.Equal("KEEP2024", tour.Identifier);
        TestAssert.Equal("Updated Name", tour.Name);
    }

    [Fact]
    public void Update_currency_without_bookings_should_succeed()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.UsDollar)));

        // Act
        var result = tour.UpdateCurrency(Currency.Euro);

        // Assert
        TestAssert.True(result.IsSuccess);
        TestAssert.Equal(Currency.Euro, tour.Pricing.Currency);
    }

    [Fact]
    public void Update_currency_with_bookings_should_fail()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.UsDollar)));
        TourUpdateGuardTestHelpers.AddBookingToTour(tour);

        // Act
        var result = tour.UpdateCurrency(Currency.Euro);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.Contains("cannot be changed if bookings exist", result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }
}
