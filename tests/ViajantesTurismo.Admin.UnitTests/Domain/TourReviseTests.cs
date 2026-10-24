using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class TourReviseTests
{
    [Fact]
    public void Revise_applies_every_change_when_all_values_are_valid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var startDate = tour.Schedule.StartDate.AddDays(30);
        var definition = new TourDefinition(
            "REVISED2027",
            "Revised Tour",
            startDate,
            startDate.AddDays(9),
            2_500m,
            400m,
            150m,
            250m,
            tour.Pricing.Currency,
            2,
            12,
            ["Guide", "Hotel"]);

        // Act
        var result = tour.Revise(definition);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        tour.Identifier.ShouldBe("REVISED2027");
        tour.Name.ShouldBe("Revised Tour");
        tour.Schedule.StartDate.ShouldBe(startDate);
        tour.Pricing.BasePrice.ShouldBe(2_500m);
        tour.Capacity.MaxCustomers.ShouldBe(12);
        tour.IncludedServices.ShouldBe(["Guide", "Hotel"]);
    }

    [Fact]
    public void Revise_leaves_the_tour_unchanged_when_any_value_is_invalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var originalName = tour.Name;
        var originalSchedule = tour.Schedule;
        var originalPricing = tour.Pricing;
        var definition = new TourDefinition(
            tour.Identifier,
            "Renamed Tour",
            tour.Schedule.StartDate,
            tour.Schedule.EndDate,
            2_500m,
            tour.Pricing.SingleRoomSupplementPrice,
            tour.Pricing.RegularBikePrice,
            tour.Pricing.EBikePrice,
            tour.Pricing.Currency,
            0,
            0,
            tour.IncludedServices);

        // Act
        var result = tour.Revise(definition);

        // Assert
        result.IsFailure.ShouldBeTrue();
        tour.Name.ShouldBe(originalName);
        tour.Schedule.ShouldBe(originalSchedule);
        tour.Pricing.ShouldBe(originalPricing);
    }
}
