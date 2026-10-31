using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Application.Bookings.Pipeline;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Application.Bookings;

public sealed class BookingIdGuardBehaviorTests
{
    [Fact]
    public async Task Handle_passes_a_result_through_to_next_for_a_real_booking_id()
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
    public async Task Handle_passes_a_value_result_through_to_next_for_a_real_booking_id()
    {
        // Arrange
        var behavior = new BookingIdGuardBehavior<StubBookingValueCommand, Result<Guid>>();
        var expected = Guid.NewGuid();

        // Act
        var result = await behavior.Handle(
            new StubBookingValueCommand(Guid.NewGuid()),
            () => ValueTask.FromResult(Result.Ok(expected)),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_short_circuits_with_booking_not_found_for_an_empty_booking_id()
    {
        // Arrange
        var behavior = new BookingIdGuardBehavior<CancelBookingCommand, Result>();
        var called = false;

        // Act
        var result = await behavior.Handle(
            new CancelBookingCommand(Guid.Empty),
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
    public async Task Handle_short_circuits_a_value_result_with_booking_not_found_for_an_empty_booking_id()
    {
        // Arrange
        var behavior = new BookingIdGuardBehavior<StubBookingValueCommand, Result<Guid>>();
        var called = false;

        // Act
        var result = await behavior.Handle(
            new StubBookingValueCommand(Guid.Empty),
            () =>
            {
                called = true;
                return ValueTask.FromResult(Result.Ok(Guid.NewGuid()));
            },
            TestContext.Current.CancellationToken);

        // Assert
        called.ShouldBeFalse();
        result.Status.ShouldBe(ResultStatus.NotFound);
        result.ErrorDetails.ShouldBe(BookingErrors.BookingNotFound().ErrorDetails);
    }
}
