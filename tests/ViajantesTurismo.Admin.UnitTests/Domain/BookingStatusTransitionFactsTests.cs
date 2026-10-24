namespace ViajantesTurismo.Admin.UnitTests.Domain;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, "Regression")]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
public sealed class BookingStatusTransitionFactsTests
{
    [Fact]
    public void Pending_booking_can_be_confirmed_or_cancelled_but_not_completed()
    {
        // Arrange
        var booking = BookingDomainTestDataFactory.CreateSingleBooking();

        // Act
        var canBeConfirmed = booking.CanBeConfirmed;
        var canBeCompleted = booking.CanBeCompleted;
        var canBeCancelled = booking.CanBeCancelled;

        // Assert
        canBeConfirmed.ShouldBeTrue();
        canBeCompleted.ShouldBeFalse();
        canBeCancelled.ShouldBeTrue();
    }

    [Fact]
    public void Confirmed_booking_can_be_completed_or_cancelled_but_not_confirmed_again()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var canBeConfirmed = booking.CanBeConfirmed;
        var canBeCompleted = booking.CanBeCompleted;
        var canBeCancelled = booking.CanBeCancelled;

        // Assert
        canBeConfirmed.ShouldBeFalse();
        canBeCompleted.ShouldBeTrue();
        canBeCancelled.ShouldBeTrue();
    }

    [Fact]
    public void Cancelled_booking_has_no_status_transition_left()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        tour.CancelBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var canBeConfirmed = booking.CanBeConfirmed;
        var canBeCompleted = booking.CanBeCompleted;
        var canBeCancelled = booking.CanBeCancelled;

        // Assert
        canBeConfirmed.ShouldBeFalse();
        canBeCompleted.ShouldBeFalse();
        canBeCancelled.ShouldBeFalse();
    }

    [Fact]
    public void Completed_booking_has_no_status_transition_left()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking();
        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        tour.CompleteBooking(booking.Id).IsSuccess.ShouldBeTrue();

        // Act
        var canBeConfirmed = booking.CanBeConfirmed;
        var canBeCompleted = booking.CanBeCompleted;
        var canBeCancelled = booking.CanBeCancelled;

        // Assert
        canBeConfirmed.ShouldBeFalse();
        canBeCompleted.ShouldBeFalse();
        canBeCancelled.ShouldBeFalse();
    }
}
