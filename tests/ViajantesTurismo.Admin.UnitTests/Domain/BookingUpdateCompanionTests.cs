using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class BookingUpdateCompanionTests
{
    [Fact]
    public void UpdateCompanion_when_booking_is_cancelled_returns_conflict_and_does_not_add_companion()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking();
        var companionCustomer = BookingDomainTestDataFactory.CreateValidCompanionCustomer();
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
    public void UpdateCompanion_when_booking_is_completed_returns_conflict_and_does_not_add_companion()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking();
        var companionCustomer = BookingDomainTestDataFactory.CreateValidCompanionCustomer();
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
    public void UpdateCompanion_when_companion_matches_principal_returns_invalid()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking();
        var companionCustomer = BookingDomainTestDataFactory.CreateValidCompanionCustomer(booking.PrincipalCustomer.CustomerId);

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
    public void UpdateCompanion_when_single_room_booking_has_companion_returns_invalid()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking(new SingleBookingOptions(RoomType: RoomType.SingleOccupancy));
        var companionCustomer = BookingDomainTestDataFactory.CreateValidCompanionCustomer();

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
    public void UpdateCompanion_when_companion_matches_principal_on_single_room_returns_aggregated_validation_errors()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking(new SingleBookingOptions(RoomType: RoomType.SingleOccupancy));
        var companionCustomer = BookingDomainTestDataFactory.CreateValidCompanionCustomer(booking.PrincipalCustomer.CustomerId);

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Multiple validation errors occurred.", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Single(result.ErrorDetails.ValidationErrors);
        Assert.Equal(2, result.ErrorDetails.ValidationErrors["companionCustomerId"].Count);
        Assert.Contains(
            "Principal and companion customers cannot be the same person.",
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        Assert.Contains(
            "Single room cannot have a companion.",
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        Assert.Null(booking.CompanionCustomer);
    }

    [Fact]
    public void UpdateCompanion_when_double_room_and_valid_companion_succeeds()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking();
        var companionCustomer = BookingDomainTestDataFactory.CreateValidCompanionCustomer();

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
    public void UpdateCompanion_when_companion_is_null_removes_existing_companion()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateDoubleBooking();
        Assert.NotNull(booking.CompanionCustomer);

        // Act
        var result = booking.UpdateCompanion(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(booking.CompanionCustomer);
    }

}
