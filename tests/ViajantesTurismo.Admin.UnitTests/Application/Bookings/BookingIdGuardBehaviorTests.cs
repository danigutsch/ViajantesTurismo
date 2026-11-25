using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Application.Bookings.Pipeline;
using ViajantesTurismo.Admin.Application.Tours.DeleteTour;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Application.Bookings;

[TestFocus(TestFocus.CoreBehavior)]
public sealed class BookingIdGuardBehaviorTests
{
    [Fact]
    public async Task Handle_passes_a_result_through_to_next_for_a_real_booking_ID()
    {
        // Arrange
        var behavior = new BookingIdGuardBehavior<CancelBookingCommand, Result>();
        var called = false;

        // Act
        var result = await behavior.Handle(
            new CancelBookingCommand(Guid.NewGuid()),
            () =>
            {
                called = true;
                return ValueTask.FromResult(Result.Ok());
            },
            TestContext.Current.CancellationToken);

        // Assert
        called.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_short_circuits_with_booking_not_found_for_an_empty_booking_ID()
    {
        // Arrange
        var behavior = new BookingIdGuardBehavior<ConfirmBookingCommand, Result>();
        var called = false;

        // Act
        var result = await behavior.Handle(
            new ConfirmBookingCommand(Guid.Empty),
            () =>
            {
                called = true;
                return ValueTask.FromResult(Result.Ok());
            },
            TestContext.Current.CancellationToken);

        // Assert
        called.ShouldBeFalse();
        result.Status.ShouldBe(ResultStatus.NotFound);
        result.ShouldBe(BookingErrors.BookingNotFound());
    }

    [Fact]
    public async Task Handle_passes_an_unrelated_command_through_with_an_empty_ID()
    {
        // Arrange
        var behavior = new BookingIdGuardBehavior<DeleteTourCommand, Result>();
        var nextResult = Result.Ok();
        var called = false;

        // Act
        var result = await behavior.Handle(
            new DeleteTourCommand(Guid.Empty, "admin-subject", DateTimeOffset.UnixEpoch),
            () =>
            {
                called = true;
                return ValueTask.FromResult(nextResult);
            },
            TestContext.Current.CancellationToken);

        // Assert
        called.ShouldBeTrue();
        result.ShouldBe(nextResult);
    }
}
