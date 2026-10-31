using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Application.Pipeline;

namespace ViajantesTurismo.Admin.UnitTests.Application.Pipeline;

public sealed class CancellationGuardBehaviorTests
{
    private static readonly CancelBookingCommand Command = new(Guid.NewGuid());

    [Fact]
    public async Task Handle_invokes_next_when_not_cancelled()
    {
        var behavior = new CancellationGuardBehavior<CancelBookingCommand, Result>();
        var called = false;

        var result = await behavior.Handle(
            Command,
            () =>
            {
                called = true;
                return ValueTask.FromResult(Result.Ok());
            },
            TestContext.Current.CancellationToken);

        called.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_throws_without_invoking_next_when_already_cancelled()
    {
        var behavior = new CancellationGuardBehavior<CancelBookingCommand, Result>();
        var called = false;
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = async () => await behavior.Handle(
            Command,
            () =>
            {
                called = true;
                return ValueTask.FromResult(Result.Ok());
            },
            cts.Token);

        await act.ShouldThrow<OperationCanceledException>();

        called.ShouldBeFalse();
    }
}
