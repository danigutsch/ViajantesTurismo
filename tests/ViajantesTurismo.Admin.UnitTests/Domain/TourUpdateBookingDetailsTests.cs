using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourUpdateBookingDetailsTests
{
    [Fact]
    public void UpdateBookingDetails_when_booking_does_not_exist_returns_not_found()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();

        // Act
        var result = tour.UpdateBookingDetails(
            Guid.CreateVersion7(),
            RoomType.DoubleOccupancy,
            BikeType.Regular,
            null,
            null);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.NotFound);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("not found in this tour", StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDetails_when_room_type_is_invalid_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            (RoomType)999,
            BikeType.Regular,
            null,
            null);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid room type", StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDetails_when_principal_bike_type_is_invalid_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.DoubleOccupancy,
            (BikeType)999,
            null,
            null);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid bike type", StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDetails_when_companion_bike_type_is_invalid_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.DoubleOccupancy,
            BikeType.Regular,
            Guid.CreateVersion7(),
            (BikeType)999);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid bike type", StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDetails_when_companion_bike_type_is_provided_without_companion_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.DoubleOccupancy,
            BikeType.Regular,
            null,
            BikeType.EBike);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot be specified without a companion customer", StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDetails_when_companion_is_provided_without_bike_type_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.DoubleOccupancy,
            BikeType.Regular,
            Guid.CreateVersion7(),
            null);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Companion bike type is required", StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDetails_when_principal_and_companion_are_the_same_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.DoubleOccupancy,
            BikeType.Regular,
            booking.PrincipalCustomer.CustomerId,
            BikeType.EBike);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot be the same person", StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDetails_when_booking_is_cancelled_returns_conflict()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        (booking.Cancel().IsSuccess).ShouldBeTrue();

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.DoubleOccupancy,
            BikeType.EBike,
            null,
            null);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Conflict);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot be modified", StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDetails_when_request_is_valid_adds_companion_and_updates_principal_bike()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        var companionCustomerId = Guid.CreateVersion7();

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.DoubleOccupancy,
            BikeType.EBike,
            companionCustomerId,
            BikeType.Regular);

        // Assert
        (result.IsSuccess).ShouldBeTrue();
        (booking.RoomType).ShouldBe(RoomType.DoubleOccupancy);
        (booking.PrincipalCustomer.BikeType).ShouldBe(BikeType.EBike);
        (booking.CompanionCustomer).ShouldNotBeNull();
        (booking.CompanionCustomer.CustomerId).ShouldBe(companionCustomerId);
        (booking.CompanionCustomer.BikeType).ShouldBe(BikeType.Regular);
    }

    [Fact]
    public void UpdateBookingDetails_when_request_removes_companion_allows_single_room()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithDoubleBooking();
        (booking.CompanionCustomer).ShouldNotBeNull();

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.SingleOccupancy,
            BikeType.Regular,
            null,
            null);

        // Assert
        (result.IsSuccess).ShouldBeTrue();
        (booking.RoomType).ShouldBe(RoomType.SingleOccupancy);
        (booking.CompanionCustomer).ShouldBeNull();
        (booking.PrincipalCustomer.BikeType).ShouldBe(BikeType.Regular);
    }

}
