using System.Diagnostics;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Application.Pipeline;

namespace ViajantesTurismo.Admin.UnitTests.Application.Pipeline;

public sealed class ResultTagBehaviorTests
{
    [Fact]
    public async Task Handle_tags_the_current_activity_ok_for_a_successful_result()
    {
        // Arrange
        var behavior = new ResultTagBehavior<CancelBookingCommand, Result>();
        using var source = new ActivitySource("result-tag-tests");
        using var listener = ResultTagBehaviorTestsHelpers.CreateListener(source);
        using var activity = source.StartActivity("request");

        // Act
        var result = await behavior.Handle(
            new CancelBookingCommand(Guid.NewGuid()),
            () => ValueTask.FromResult(Result.Ok()),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        activity.ShouldNotBeNull().GetTagItem(ResultTagBehavior<CancelBookingCommand, Result>.TagName).ShouldBe("ok");
    }

    [Fact]
    public async Task Handle_tags_the_current_activity_failed_for_a_failed_result()
    {
        // Arrange
        var behavior = new ResultTagBehavior<CancelBookingCommand, Result>();
        using var source = new ActivitySource("result-tag-tests");
        using var listener = ResultTagBehaviorTestsHelpers.CreateListener(source);
        using var activity = source.StartActivity("request");

        // Act
        var result = await behavior.Handle(
            new CancelBookingCommand(Guid.NewGuid()),
            () => ValueTask.FromResult(Result.NotFound("missing")),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        activity.ShouldNotBeNull().GetTagItem(ResultTagBehavior<CancelBookingCommand, Result>.TagName).ShouldBe("failed");
    }

    [Fact]
    public async Task Handle_returns_the_result_when_no_activity_is_listening()
    {
        // Arrange
        var behavior = new ResultTagBehavior<CancelBookingCommand, Result>();

        // Act
        var result = await behavior.Handle(
            new CancelBookingCommand(Guid.NewGuid()),
            () => ValueTask.FromResult(Result.Ok()),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
