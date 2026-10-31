using PandaBear.Testing;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests;

[Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[TestScope(TestScope.Unit)]
public sealed class RedirectDelayTests
{
    [Fact]
    public async Task Elapsed_reports_true_when_the_delay_completes_without_cancellation()
    {
        // Arrange
        using var source = new CancellationTokenSource();

        // Act
        var elapsed = await RedirectDelay.Elapsed(0, source.Token);

        // Assert
        elapsed.ShouldBeTrue();
    }

    [Fact]
    public async Task Elapsed_reports_false_when_the_user_canceled_before_the_wait()
    {
        // Arrange
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // Act
        var elapsed = await RedirectDelay.Elapsed(60_000, source.Token);

        // Assert
        elapsed.ShouldBeFalse();
    }

    [Fact]
    public async Task Elapsed_reports_false_when_the_user_cancels_during_the_wait()
    {
        // Arrange
        using var source = new CancellationTokenSource();
        var waiting = RedirectDelay.Elapsed(60_000, source.Token);

        // Act
        await source.CancelAsync();
        var elapsed = await waiting;

        // Assert
        elapsed.ShouldBeFalse();
    }
}
