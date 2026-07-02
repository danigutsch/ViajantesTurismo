using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class NoOpMediaUploadScannerTests
{
    [Fact]
    public async Task Scan_returns_disabled_status()
    {
        // Arrange
        var scanner = new NoOpMediaUploadScanner();
        await using var content = new MemoryStream([1]);

        // Act
        var result = await scanner.Scan(
            new MediaUploadScanRequest("images/photo.jpg", content, "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(MediaUploadScanStatus.Disabled);
    }

    [Fact]
    public async Task Scan_observes_cancellation()
    {
        // Arrange
        var scanner = new NoOpMediaUploadScanner();
        await using var content = new MemoryStream([1]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> action = async () => await scanner.Scan(
            new MediaUploadScanRequest("images/photo.jpg", content, "image/jpeg", 1),
            cancellation.Token);

        // Assert
        var exception = await action.ShouldThrow<OperationCanceledException>();
        exception.CancellationToken.ShouldBe(cancellation.Token);
    }

    [Fact]
    public void Scan_rejects_null_request()
    {
        // Arrange
        var scanner = new NoOpMediaUploadScanner();
        var method = typeof(NoOpMediaUploadScanner).GetMethod(nameof(NoOpMediaUploadScanner.Scan)).ShouldNotBeNull();

        // Act
        Action action = () => method.Invoke(scanner, [null, TestContext.Current.CancellationToken]);

        // Assert
        var exception = action.ShouldThrowInner<ArgumentNullException>();
        exception.ParamName.ShouldBe("request");
    }
}
