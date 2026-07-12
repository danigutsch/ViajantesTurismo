using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class ClamAvMediaUploadScannerTests
{
    [Fact]
    public async Task Scan_returns_passed_when_daemon_reports_clean_content()
    {
        // Arrange
        await using var server = new ClamAvTestServer("stream: OK\0");
        var scanner = new ClamAvMediaUploadScanner(Options.Create(new ClamAvMediaUploadScannerOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = server.Port
        }));
        var content = Encoding.UTF8.GetBytes("safe image bytes");

        // Act
        var result = await scanner.Scan(new MediaUploadScanRequest("media/test/original.jpg", new MemoryStream(content), "image/jpeg", content.Length), TestContext.Current.CancellationToken);
        var received = await server.Completion;
        var expected = new byte[10 + sizeof(int) + content.Length + sizeof(int)];
        Encoding.ASCII.GetBytes("zINSTREAM\0", expected);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(expected.AsSpan(10, sizeof(int)), content.Length);
        content.CopyTo(expected, 10 + sizeof(int));

        // Assert
        result.Status.ShouldBe(MediaUploadScanStatus.Passed);
        received.ShouldBe(expected);
    }

    [Fact]
    public async Task Scan_returns_rejected_when_daemon_reports_malware()
    {
        // Arrange
        await using var server = new ClamAvTestServer("stream: Eicar-Test-Signature FOUND\0");
        var scanner = new ClamAvMediaUploadScanner(Options.Create(new ClamAvMediaUploadScannerOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = server.Port
        }));
        var content = Encoding.UTF8.GetBytes("malware fixture");

        // Act
        var result = await scanner.Scan(new MediaUploadScanRequest("media/test/original.jpg", new MemoryStream(content), "image/jpeg", content.Length), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(MediaUploadScanStatus.Rejected);
        result.Message.ShouldBeNull();
    }

    [Fact]
    public async Task Scan_fails_closed_when_daemon_is_unavailable()
    {
        // Arrange
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var scanner = new ClamAvMediaUploadScanner(Options.Create(new ClamAvMediaUploadScannerOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            Timeout = TimeSpan.FromMilliseconds(100)
        }));
        var content = Encoding.UTF8.GetBytes("safe image bytes");

        // Act
        var result = await scanner.Scan(new MediaUploadScanRequest("media/test/original.jpg", new MemoryStream(content), "image/jpeg", content.Length), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(MediaUploadScanStatus.Failed);
        result.Message.ShouldBeNull();
    }

    [Fact]
    public async Task Scan_fails_closed_when_daemon_reports_an_error()
    {
        // Arrange
        await using var server = new ClamAvTestServer("stream: scan failed ERROR\0");
        var scanner = new ClamAvMediaUploadScanner(Options.Create(new ClamAvMediaUploadScannerOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = server.Port
        }));
        var content = Encoding.UTF8.GetBytes("safe image bytes");

        // Act
        var result = await scanner.Scan(new MediaUploadScanRequest("media/test/original.jpg", new MemoryStream(content), "image/jpeg", content.Length), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(MediaUploadScanStatus.Failed);
        result.Message.ShouldBeNull();
    }

    [Fact]
    public async Task Scan_fails_closed_when_response_is_not_a_valid_clamav_instream_response()
    {
        // Arrange
        await using var server = new ClamAvTestServer("unexpected OK\0");
        var scanner = new ClamAvMediaUploadScanner(Options.Create(new ClamAvMediaUploadScannerOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = server.Port
        }));
        var content = Encoding.UTF8.GetBytes("safe image bytes");

        // Act
        var result = await scanner.Scan(new MediaUploadScanRequest("media/test/original.jpg", new MemoryStream(content), "image/jpeg", content.Length), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(MediaUploadScanStatus.Failed);
    }

    [Fact]
    public async Task Scan_fails_closed_when_terminated_response_exceeds_the_maximum_size()
    {
        // Arrange
        await using var server = new ClamAvTestServer($"stream: {new string('a', 4 * 1024)} OK\0");
        var scanner = new ClamAvMediaUploadScanner(Options.Create(new ClamAvMediaUploadScannerOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = server.Port
        }));
        var content = Encoding.UTF8.GetBytes("safe image bytes");

        // Act
        var result = await scanner.Scan(new MediaUploadScanRequest("media/test/original.jpg", new MemoryStream(content), "image/jpeg", content.Length), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(MediaUploadScanStatus.Failed);
    }

    [Fact]
    public async Task Scan_fails_closed_when_daemon_does_not_respond_before_timeout()
    {
        // Arrange
        await using var server = new ClamAvTestServer(response: null);
        var scanner = new ClamAvMediaUploadScanner(Options.Create(new ClamAvMediaUploadScannerOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = server.Port,
            Timeout = TimeSpan.FromMilliseconds(100)
        }));
        var content = Encoding.UTF8.GetBytes("safe image bytes");

        // Act
        var result = await scanner.Scan(new MediaUploadScanRequest("media/test/original.jpg", new MemoryStream(content), "image/jpeg", content.Length), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(MediaUploadScanStatus.Failed);
    }
}
