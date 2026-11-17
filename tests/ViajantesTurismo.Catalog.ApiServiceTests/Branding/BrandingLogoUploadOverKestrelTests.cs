using ImageMagick;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using PandaBear.Testing;
using PandaBear.Testing.MalwareScanning;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.UploadWire.Testing;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Branding;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.AreaName, TestTraits.BrandingArea)]
public sealed class BrandingLogoUploadOverKestrelTests
{
    [Fact]
    public async Task Declared_body_over_the_logo_limit_returns_413_before_reading()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scanner = TestMalwareScanner.Clean();
        await using var scenario = BrandingLogoKestrelScenario.Start(scanner);
        using var upload = new RawHttpUpload();
        var body = new byte[BrandingLogoLimits.MaxUploadBytes + 1];

        // Act
        await scenario.SendPartialUpload(upload, body, bytesToSend: 0, ct);
        var response = await upload.ReadStatusLine(ct).WaitAsync(WireTimeout.Default, ct);

        // Assert
        response.ShouldBe("HTTP/1.1 413 Payload Too Large");
        scenario.Probe.BytesRead.ShouldBe(0);
        scanner.CallCount.ShouldBe(0);
        scenario.Store.Logos.ShouldBeEmpty();
        scenario.Store.WriteCount.ShouldBe(0);
    }

    [Fact]
    public async Task Chunked_body_over_the_logo_limit_returns_413_without_scanning()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scanner = TestMalwareScanner.Clean();
        await using var scenario = BrandingLogoKestrelScenario.Start(scanner);
        var body = new byte[BrandingLogoLimits.MaxUploadBytes + 1];
        var chunks = new GatedChunkContent(body, BrandingLogoLimits.MaxUploadBytes / 2, "image/png");
        await using var upload = scenario.StartUpload(chunks, ct, chunked: true);

        // Act
        chunks.ReleaseChunk();
        await scenario.Probe.WaitForBytes(chunks.BytesAfter(1)).WaitAsync(WireTimeout.Default, ct);
        chunks.ReleaseChunk();
        chunks.ReleaseChunk();
        using var response = await upload.Response;

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        scenario.Probe.BytesRead.ShouldBeGreaterThanOrEqualTo(chunks.BytesAfter(1));
        scenario.Probe.BytesRead.ShouldBeLessThanOrEqualTo(BrandingLogoLimits.MaxUploadBytes);
        scanner.CallCount.ShouldBe(0);
        scenario.Store.Logos.ShouldBeEmpty();
        scenario.Store.WriteCount.ShouldBe(0);
    }

    [Fact]
    public async Task Client_disconnect_during_the_body_ends_the_request_without_storing_or_scanning()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scanner = TestMalwareScanner.Clean();
        await using var scenario = BrandingLogoKestrelScenario.Start(scanner);
        using var upload = new RawHttpUpload();
        var body = BrandingTestImages.CreateUncompressedLogoPng();
        const int prefixLength = 384 * 1024;
        await scenario.SendPartialUpload(upload, body, prefixLength, ct);
        await scenario.Probe.WaitForBytes(prefixLength).WaitAsync(WireTimeout.Default, ct);

        // Act
        upload.Abort();
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, ct);

        // Assert
        scenario.Probe.DisconnectObserved.ShouldBeTrue();
        scanner.CallCount.ShouldBe(0);
        scenario.Store.Logos.ShouldBeEmpty();
        scenario.Store.WriteCount.ShouldBe(0);
    }

    [Fact]
    public async Task Stalled_body_returns_408_without_scanning_or_storing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scanner = TestMalwareScanner.Clean();
        var rate = new MinDataRate(bytesPerSecond: 1_000_000, gracePeriod: TimeSpan.FromMilliseconds(1_001));
        await using var scenario = BrandingLogoKestrelScenario.Start(scanner, rate);
        using var upload = new RawHttpUpload();
        var body = BrandingTestImages.Create(MagickFormat.Png, 64, 32);
        var prefixLength = body.Length / 2;

        // Act
        await scenario.SendPartialUpload(upload, body, prefixLength, ct);
        await scenario.Probe.WaitForBytes(prefixLength).WaitAsync(WireTimeout.Default, ct);
        var response = await upload.ReadStatusLine(ct).WaitAsync(WireTimeout.Default, ct);
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, ct);

        // Assert
        response.ShouldBe("HTTP/1.1 408 Request Timeout");
        scanner.CallCount.ShouldBe(0);
        scenario.Store.Logos.ShouldBeEmpty();
        scenario.Store.WriteCount.ShouldBe(0);
    }

    [Fact]
    public async Task Scan_starts_only_after_the_last_chunk_and_receives_the_original_image()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture };
        await using var scenario = BrandingLogoKestrelScenario.Start(scanner);
        var body = BrandingTestImages.Create(MagickFormat.Png, 64, 32);
        var chunks = new GatedChunkContent(body, chunkSize: 64, "image/png");
        await using var upload = scenario.StartUpload(chunks, ct);

        // Act
        for (var chunk = 1; chunk < chunks.ChunkCount; chunk++)
        {
            chunks.ReleaseChunk();
            await scenario.Probe.WaitForBytes(chunks.BytesAfter(chunk)).WaitAsync(WireTimeout.Default, ct);
            scanner.CallCount.ShouldBe(0);
        }

        chunks.ReleaseChunk();
        using var response = await upload.Response;

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var scan = scanner.Scans.ShouldHaveSingleItem();
        scan.Content.Span.SequenceEqual(body).ShouldBeTrue();
        scan.Stream.CanRead.ShouldBeFalse();
        scenario.Probe.LargestRequestedRead.ShouldBeLessThanOrEqualTo(81_920);
        scenario.Store.Logos.ShouldHaveSingleItem();
        scenario.Store.WriteCount.ShouldBe(1);
    }

    [Fact]
    public async Task Client_cancellation_during_scanning_disposes_the_spooled_lease_and_leaves_no_logo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scanner = new BrandingLeaseCancellationScanner();
        await using var scenario = BrandingLogoKestrelScenario.Start(scanner);
        using var upload = new RawHttpUpload();
        var body = BrandingTestImages.CreateUncompressedLogoPng();
        await scenario.SendPartialUpload(upload, body, body.Length, ct);
        var lease = await scanner.Entered.WaitAsync(WireTimeout.Default, ct);

        // Act
        upload.Abort();
        await scanner.Cancelled.WaitAsync(WireTimeout.Default, ct);
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, ct);

        // Assert
        scenario.Probe.DisconnectObserved.ShouldBeTrue();
        var file = lease.ShouldBeOfType<FileStream>();
        file.CanRead.ShouldBeFalse();
        File.Exists(file.Name).ShouldBeFalse();
        scenario.Store.Logos.ShouldBeEmpty();
        scenario.Store.WriteCount.ShouldBe(0);
    }
}
