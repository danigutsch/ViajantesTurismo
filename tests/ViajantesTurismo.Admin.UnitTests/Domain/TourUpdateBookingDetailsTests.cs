using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourUpdateBookingDetailsTests
{
    [Fact]
    public void UpdateBookingDetails_When_Booking_Does_Not_Exist_Returns_Not_Found()
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
    public void UpdateBookingDetails_When_Room_Type_Is_Invalid_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void UpdateBookingDetails_When_Principal_Bike_Type_Is_Invalid_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void UpdateBookingDetails_When_Companion_Bike_Type_Is_Invalid_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void UpdateBookingDetails_When_Companion_Bike_Type_Is_Provided_Without_Companion_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void UpdateBookingDetails_When_Companion_Is_Provided_Without_Bike_Type_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void UpdateBookingDetails_When_Principal_And_Companion_Are_The_Same_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void UpdateBookingDetails_When_Booking_Is_Cancelled_Returns_Conflict()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();
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
    public void UpdateBookingDetails_When_Request_Is_Valid_Adds_Companion_And_Updates_Principal_Bike()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();
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
    public void UpdateBookingDetails_When_Request_Removes_Companion_Allows_Single_Room()
    {
        // Arrange
        var (tour, booking) = CreateDoubleBooking();
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

    private static (Tour Tour, Booking Booking) CreateSingleBooking()
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour);

        Assert.True(bookingResult.IsSuccess, "Failed to create single booking for test setup.");
        return (tour, bookingResult.Value);
    }

    private static (Tour Tour, Booking Booking) CreateDoubleBooking()
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddDoubleCustomerBooking(tour, null);

        Assert.True(bookingResult.IsSuccess, "Failed to create double booking for test setup.");
        return (tour, bookingResult.Value);
    }
}
