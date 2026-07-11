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
        (result.IsSuccess).ShouldBeTrue();
        (tour.Identifier).ShouldBe("NEWID");
        (tour.Name).ShouldBe("New Name");
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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails!.Detail).ShouldContain("cannot be changed if bookings exist", StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeTrue();
        (tour.Identifier).ShouldBe("KEEP2024");
        (tour.Name).ShouldBe("Updated Name");
    }

    [Fact]
    public void Update_currency_without_bookings_should_succeed()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.UsDollar)));

        // Act
        var result = tour.UpdateCurrency(Currency.Euro);

        // Assert
        (result.IsSuccess).ShouldBeTrue();
        (tour.Pricing.Currency).ShouldBe(Currency.Euro);
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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails!.Detail).ShouldContain("cannot be changed if bookings exist", StringComparison.Ordinal);
    }
}
