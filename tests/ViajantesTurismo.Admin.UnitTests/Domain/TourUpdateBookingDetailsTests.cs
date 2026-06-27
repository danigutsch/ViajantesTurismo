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
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("not found in this tour", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid room type", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid bike type", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid bike type", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot be specified without a companion customer", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Companion bike type is required", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot be the same person", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDetails_when_booking_is_cancelled_returns_conflict()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        Assert.True(booking.Cancel().IsSuccess);

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.DoubleOccupancy,
            BikeType.EBike,
            null,
            null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot be modified", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        Assert.True(result.IsSuccess);
        Assert.Equal(RoomType.DoubleOccupancy, booking.RoomType);
        Assert.Equal(BikeType.EBike, booking.PrincipalCustomer.BikeType);
        Assert.NotNull(booking.CompanionCustomer);
        Assert.Equal(companionCustomerId, booking.CompanionCustomer.CustomerId);
        Assert.Equal(BikeType.Regular, booking.CompanionCustomer.BikeType);
    }

    [Fact]
    public void UpdateBookingDetails_when_request_removes_companion_allows_single_room()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithDoubleBooking();
        Assert.NotNull(booking.CompanionCustomer);

        // Act
        var result = tour.UpdateBookingDetails(
            booking.Id,
            RoomType.SingleOccupancy,
            BikeType.Regular,
            null,
            null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RoomType.SingleOccupancy, booking.RoomType);
        Assert.Null(booking.CompanionCustomer);
        Assert.Equal(BikeType.Regular, booking.PrincipalCustomer.BikeType);
    }

}
