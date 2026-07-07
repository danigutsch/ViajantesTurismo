namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

public sealed class EventuallyTests
{
    [Fact]
    public async Task Until_returns_first_non_null_probe_result()
    {
        // Arrange
        var attempts = 0;

        // Act
        var result = await Eventually.Until(
            _ => Task.FromResult(++attempts == 2 ? "ready" : null),
            TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe("ready");
        attempts.ShouldBe(2);
    }

    [Fact]
    public async Task Until_translates_timeout_cancellation_to_timeout_exception()
    {
        // Arrange
        var attempts = 0;

        // Act
        var wait = () => Eventually.Until(
            _ =>
            {
                attempts++;
                return Task.FromResult<string?>(null);
            },
            TimeSpan.FromMilliseconds(25),
            TestContext.Current.CancellationToken);

        // Assert
        var exception = await wait.ShouldThrow<TimeoutException>();
        exception.Message.ShouldContain("Expected condition was not met", StringComparison.Ordinal);
        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task Until_preserves_caller_cancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var wait = () => Eventually.Until(
            _ => Task.FromResult<string?>(null),
            TimeSpan.FromSeconds(1),
            cts.Token);

        // Assert
        await wait.ShouldThrow<OperationCanceledException>();
    }
}
