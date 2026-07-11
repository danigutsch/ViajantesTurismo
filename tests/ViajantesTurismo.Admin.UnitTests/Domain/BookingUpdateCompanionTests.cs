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
        TestAssert.True(cancelResult.IsSuccess);

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Conflict, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("cannot be modified", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Null(booking.CompanionCustomer);
    }

    [Fact]
    public void UpdateCompanion_when_booking_is_completed_returns_conflict_and_does_not_add_companion()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking();
        var companionCustomer = BookingDomainTestDataFactory.CreateValidCompanionCustomer();
        TestAssert.True(booking.Confirm().IsSuccess);
        TestAssert.True(booking.Complete().IsSuccess);

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Conflict, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("cannot be modified", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Null(booking.CompanionCustomer);
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
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Invalid, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.Equal(
            ["Principal and companion customers cannot be the same person."],
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        TestAssert.Null(booking.CompanionCustomer);
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
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Invalid, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.Equal(
            ["Single room cannot have a companion."],
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        TestAssert.Null(booking.CompanionCustomer);
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
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Invalid, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Equal("Multiple validation errors occurred.", result.ErrorDetails.Detail);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.ExactlyOne(result.ErrorDetails.ValidationErrors);
        TestAssert.Equal(2, result.ErrorDetails.ValidationErrors["companionCustomerId"].Count);
        TestAssert.Contains(
            "Principal and companion customers cannot be the same person.",
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        TestAssert.Contains(
            "Single room cannot have a companion.",
            result.ErrorDetails.ValidationErrors["companionCustomerId"]);
        TestAssert.Null(booking.CompanionCustomer);
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
        TestAssert.True(result.IsSuccess);
        TestAssert.NotNull(booking.CompanionCustomer);
        TestAssert.Equal(companionCustomer.CustomerId, booking.CompanionCustomer.CustomerId);
        TestAssert.Equal(companionCustomer.BikeType, booking.CompanionCustomer.BikeType);
        TestAssert.Equal(companionCustomer.BikePrice, booking.CompanionCustomer.BikePrice);
    }

    [Fact]
    public void UpdateCompanion_when_companion_is_null_removes_existing_companion()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateDoubleBooking();
        TestAssert.NotNull(booking.CompanionCustomer);

        // Act
        var result = booking.UpdateCompanion(null);

        // Assert
        TestAssert.True(result.IsSuccess);
        TestAssert.Null(booking.CompanionCustomer);
    }

}
