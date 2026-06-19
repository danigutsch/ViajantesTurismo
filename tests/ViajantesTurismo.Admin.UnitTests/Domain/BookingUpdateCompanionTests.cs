using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class BookingUpdateCompanionTests
{
    [Fact]
    public void UpdateCompanion_When_Booking_Is_Cancelled_Returns_Conflict_And_Does_Not_Add_Companion()
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
    public void UpdateCompanion_When_Booking_Is_Completed_Returns_Conflict_And_Does_Not_Add_Companion()
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
    public void UpdateCompanion_When_Companion_Matches_Principal_Returns_Invalid()
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
    public void UpdateCompanion_When_Single_Room_Booking_Has_Companion_Returns_Invalid()
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
    public void UpdateCompanion_When_Companion_Matches_Principal_On_Single_Room_Returns_Aggregated_Validation_Errors()
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
    public void UpdateCompanion_When_Double_Room_And_Valid_Companion_Succeeds()
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
    public void UpdateCompanion_When_Companion_Is_Null_Removes_Existing_Companion()
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
