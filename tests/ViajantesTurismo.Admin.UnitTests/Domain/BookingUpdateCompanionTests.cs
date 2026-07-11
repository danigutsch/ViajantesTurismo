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
        (cancelResult.IsSuccess).ShouldBeTrue();

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Conflict);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot be modified", StringComparison.Ordinal);
        (booking.CompanionCustomer).ShouldBeNull();
    }

    [Fact]
    public void UpdateCompanion_when_booking_is_completed_returns_conflict_and_does_not_add_companion()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking();
        var companionCustomer = BookingDomainTestDataFactory.CreateValidCompanionCustomer();
        (booking.Confirm().IsSuccess).ShouldBeTrue();
        (booking.Complete().IsSuccess).ShouldBeTrue();

        // Act
        var result = booking.UpdateCompanion(companionCustomer);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Conflict);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot be modified", StringComparison.Ordinal);
        (booking.CompanionCustomer).ShouldBeNull();
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors["companionCustomerId"]).ShouldBe(["Principal and companion customers cannot be the same person."]);
        (booking.CompanionCustomer).ShouldBeNull();
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors["companionCustomerId"]).ShouldBe(["Single room cannot have a companion."]);
        (booking.CompanionCustomer).ShouldBeNull();
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldBe("Multiple validation errors occurred.");
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors).ShouldHaveSingleItem();
        (result.ErrorDetails.ValidationErrors["companionCustomerId"].Count).ShouldBe(2);
        (result.ErrorDetails.ValidationErrors["companionCustomerId"]).ShouldContain("Principal and companion customers cannot be the same person.");
        (result.ErrorDetails.ValidationErrors["companionCustomerId"]).ShouldContain("Single room cannot have a companion.");
        (booking.CompanionCustomer).ShouldBeNull();
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
        (result.IsSuccess).ShouldBeTrue();
        (booking.CompanionCustomer).ShouldNotBeNull();
        (booking.CompanionCustomer.CustomerId).ShouldBe(companionCustomer.CustomerId);
        (booking.CompanionCustomer.BikeType).ShouldBe(companionCustomer.BikeType);
        (booking.CompanionCustomer.BikePrice).ShouldBe(companionCustomer.BikePrice);
    }

    [Fact]
    public void UpdateCompanion_when_companion_is_null_removes_existing_companion()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateDoubleBooking();
        (booking.CompanionCustomer).ShouldNotBeNull();

        // Act
        var result = booking.UpdateCompanion(null);

        // Assert
        (result.IsSuccess).ShouldBeTrue();
        (booking.CompanionCustomer).ShouldBeNull();
    }

}
