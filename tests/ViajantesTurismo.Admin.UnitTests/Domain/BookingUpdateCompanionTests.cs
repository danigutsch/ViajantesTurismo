using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class BookingUpdateCompanionTests
{
    [Fact]
    public void UpdateCompanion_WhenBookingIsCancelled_ReturnsConflict_And_DoesNotAddCompanion()
    {
        // Arrange
        var booking = CreateSingleBooking();
        var companionCustomer = CreateValidCompanionCustomer();
        var cancelResult = booking.Cancel();
        Assert.True(cancelResult.IsSuccess);

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot be modified", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Null(booking.CompanionCustomer);
    }

    [Fact]
    public void UpdateCompanion_WhenBookingIsCompleted_ReturnsConflict_And_DoesNotAddCompanion()
    {
        // Arrange
        var booking = CreateSingleBooking();
        var companionCustomer = CreateValidCompanionCustomer();
        Assert.True(booking.Confirm().IsSuccess);
        Assert.True(booking.Complete().IsSuccess);

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot be modified", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Null(booking.CompanionCustomer);
    }

    [Fact]
    public void UpdateCompanion_WhenCompanionMatchesPrincipal_ReturnsInvalid()
    {
        // Arrange
        var booking = CreateSingleBooking();
        var companionCustomer = CreateValidCompanionCustomer(booking.PrincipalCustomer.CustomerId);

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Equal(
            ["Principal and companion customers cannot be the same person."],
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        Assert.Null(booking.CompanionCustomer);
    }

    [Fact]
    public void UpdateCompanion_WhenSingleRoomBookingHasCompanion_ReturnsInvalid()
    {
        // Arrange
        var booking = CreateSingleBooking(new SingleBookingOptions(RoomType: RoomType.SingleOccupancy));
        var companionCustomer = CreateValidCompanionCustomer();

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Equal(
            ["Single room cannot have a companion."],
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        Assert.Null(booking.CompanionCustomer);
    }

    [Fact]
    public void UpdateCompanion_WhenCompanionMatchesPrincipal_OnSingleRoom_ReturnsAggregatedValidationErrors()
    {
        // Arrange
        var booking = CreateSingleBooking(new SingleBookingOptions(RoomType: RoomType.SingleOccupancy));
        var companionCustomer = CreateValidCompanionCustomer(booking.PrincipalCustomer.CustomerId);

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Multiple validation errors occurred.", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Single(result.ErrorDetails.ValidationErrors);
        Assert.Equal(2, result.ErrorDetails.ValidationErrors["companionCustomerId"].Length);
        Assert.Contains(
            "Principal and companion customers cannot be the same person.",
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        Assert.Contains(
            "Single room cannot have a companion.",
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        Assert.Null(booking.CompanionCustomer);
    }

    [Fact]
    public void UpdateCompanion_WhenDoubleRoomAndValidCompanion_Succeeds()
    {
        // Arrange
        var booking = CreateSingleBooking();
        var companionCustomer = CreateValidCompanionCustomer();

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(booking.CompanionCustomer);
        Assert.Equal(companionCustomer.CustomerId, booking.CompanionCustomer.CustomerId);
        Assert.Equal(companionCustomer.BikeType, booking.CompanionCustomer.BikeType);
        Assert.Equal(companionCustomer.BikePrice, booking.CompanionCustomer.BikePrice);
    }

    [Fact]
    public void UpdateCompanion_WhenCompanionIsNull_RemovesExistingCompanion()
    {
        // Arrange
        var booking = CreateDoubleBooking();
        Assert.NotNull(booking.CompanionCustomer);

        // Act
        var result = booking.UpdateCompanion(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(booking.CompanionCustomer);
    }

    private static Booking CreateSingleBooking(SingleBookingOptions? options = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour, options);

        Assert.True(bookingResult.IsSuccess, "Failed to create single booking for test setup.");
        return bookingResult.Value;
    }

    private static Booking CreateDoubleBooking(DoubleBookingOptions? options = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddDoubleCustomerBooking(tour, options);

        Assert.True(bookingResult.IsSuccess, "Failed to create double booking for test setup.");
        return bookingResult.Value;
    }

    private static BookingCustomer CreateValidCompanionCustomer(Guid? customerId = null)
    {
        var companionCustomerResult = BookingCustomer.Create(
            customerId ?? Guid.CreateVersion7(),
            BikeType.EBike,
            200m);

        Assert.True(companionCustomerResult.IsSuccess, "Failed to create companion customer for test setup.");
        return companionCustomerResult.Value;
    }
}
