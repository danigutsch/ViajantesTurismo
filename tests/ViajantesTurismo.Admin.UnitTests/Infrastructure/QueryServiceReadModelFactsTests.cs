using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
public sealed class QueryServiceReadModelFactsTests
{
    [Fact]
    public async Task Tour_read_model_carries_the_domain_capacity_facts()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = QueryServiceReadModelScenario.Create();
        var tour = EntityBuilders.BuildTour(new TourOptions(Capacity: new TourCapacityOptions(2, 3)));
        var booking = await scenario.AddTourWithSingleBooking(tour, ct);
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        await scenario.Save(ct);

        // Act
        var dto = await scenario.QueryService.GetTourById(tour.Id, ct);

        // Assert
        dto.ShouldNotBeNull();
        dto.AvailableSpots.ShouldBe(tour.AvailableSpots);
        dto.IsFullyBooked.ShouldBe(tour.IsFullyBooked);
        dto.IsAtMinimumCapacity.ShouldBe(tour.IsAtMinimumCapacity);
        dto.AvailableSpots.ShouldBe(2);
        dto.IsFullyBooked.ShouldBeFalse();
        dto.IsAtMinimumCapacity.ShouldBeFalse();
    }

    [Fact]
    public async Task Tour_list_read_model_reports_a_fully_booked_tour()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = QueryServiceReadModelScenario.Create();
        var tour = EntityBuilders.BuildTour(new TourOptions(Capacity: new TourCapacityOptions(1, 1)));
        var booking = await scenario.AddTourWithSingleBooking(tour, ct);
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        await scenario.Save(ct);

        // Act
        var tours = await scenario.QueryService.GetAllTours(ct);

        // Assert
        var dto = tours.ShouldHaveSingleItem();
        dto.IsFullyBooked.ShouldBeTrue();
        dto.IsAtMinimumCapacity.ShouldBeTrue();
        dto.AvailableSpots.ShouldBe(0);
    }

    [Fact]
    public async Task Booking_read_model_reports_that_a_cancelled_booking_cannot_modify_booked_services()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = QueryServiceReadModelScenario.Create();
        var tour = EntityBuilders.BuildTour();
        var booking = await scenario.AddTourWithSingleBooking(tour, ct);
        tour.CancelBooking(booking.Id).IsSuccess.ShouldBeTrue();
        await scenario.Save(ct);

        // Act
        var dto = await scenario.QueryService.GetBookingById(booking.Id, ct);

        // Assert
        dto.ShouldNotBeNull();
        dto.CanModifyBookedServices.ShouldBeFalse();
        dto.CanBeConfirmed.ShouldBeFalse();
        dto.CanBeCompleted.ShouldBeFalse();
        dto.CanBeCancelled.ShouldBeFalse();
    }

    [Fact]
    public async Task Booking_read_models_carry_the_domain_status_transition_facts()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = QueryServiceReadModelScenario.Create();
        var tour = EntityBuilders.BuildTour();
        var booking = await scenario.AddTourWithSingleBooking(tour, ct);
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        await scenario.Save(ct);

        // Act
        var byId = await scenario.QueryService.GetBookingById(booking.Id, ct);
        var byTour = await scenario.QueryService.GetBookingsByTourId(tour.Id, ct);
        var byCustomer = await scenario.QueryService.GetBookingsByCustomerId(booking.PrincipalCustomer.CustomerId, ct);
        var all = await scenario.QueryService.GetAllBookings(ct);

        // Assert
        byId.ShouldNotBeNull();
        GetBookingDto[] dtos = [byId, byTour.ShouldHaveSingleItem(), byCustomer.ShouldHaveSingleItem(), all.ShouldHaveSingleItem()];
        foreach (var dto in dtos)
        {
            dto.CanBeConfirmed.ShouldBe(booking.CanBeConfirmed);
            dto.CanBeCompleted.ShouldBe(booking.CanBeCompleted);
            dto.CanBeCancelled.ShouldBe(booking.CanBeCancelled);
            dto.CanBeRemoved.ShouldBe(booking.CanBeRemoved);
        }
    }

    [Fact]
    public async Task Booking_list_read_models_report_that_a_pending_booking_can_modify_booked_services()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = QueryServiceReadModelScenario.Create();
        var tour = EntityBuilders.BuildTour();
        var booking = await scenario.AddTourWithSingleBooking(tour, ct);

        // Act
        var byTour = await scenario.QueryService.GetBookingsByTourId(tour.Id, ct);
        var byCustomer = await scenario.QueryService.GetBookingsByCustomerId(booking.PrincipalCustomer.CustomerId, ct);
        var all = await scenario.QueryService.GetAllBookings(ct);

        // Assert
        byTour.ShouldHaveSingleItem().CanModifyBookedServices.ShouldBeTrue();
        byCustomer.ShouldHaveSingleItem().CanModifyBookedServices.ShouldBeTrue();
        all.ShouldHaveSingleItem().CanModifyBookedServices.ShouldBeTrue();
    }
}
