using Microsoft.AspNetCore.Server.Kestrel.Core;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.UploadWire.Testing;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

/// <summary>
/// Issue 706: the Catalog tour image upload exercised over real HTTP on a loopback Kestrel server (not TestServer),
/// proving disconnect, slow-client and oversize behavior of the upload intake.
/// </summary>
public sealed class MediaUploadOverKestrelTests
{
    [Fact]
    public async Task Upload_ends_the_request_without_scanning_or_persisting_when_the_client_disconnects_mid_upload()
    {
        // Arrange
        await using var scenario = await MediaUploadKestrelScenario.Start();
        var image = new byte[262_144];
        MediaUploadKestrelScenario.Png.CopyTo(image, 0);
        var payload = await MediaUploadKestrelScenario.Form(image, TestContext.Current.CancellationToken);
        var partialLength = payload.Body.Length / 2;
        using var upload = new RawHttpUpload();
        await upload.Send(scenario.BaseUri, scenario.Route, scenario.Authorization, payload, partialLength, TestContext.Current.CancellationToken);
        await scenario.Probe.WaitForBytes(partialLength).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Act
        upload.Abort();
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Assert
        scenario.Probe.DisconnectObserved.ShouldBeTrue();
        scenario.Scans.Scans.ShouldBeEmpty();
        var persistedImages = await scenario.PersistedImageCount(TestContext.Current.CancellationToken);
        persistedImages.ShouldBe(0);
        var storedObjects = await scenario.StoredObjectCount(TestContext.Current.CancellationToken);
        storedObjects.ShouldBe(0);
    }

    [Fact]
    public async Task Upload_scans_the_exact_bytes_once_after_the_last_chunk_of_a_slow_client()
    {
        // Arrange
        await using var scenario = await MediaUploadKestrelScenario.Start();
        var payload = await MediaUploadKestrelScenario.Form(MediaUploadKestrelScenario.Png, TestContext.Current.CancellationToken);
        using var upload = new SlowUpload(scenario.Client, new Uri(scenario.Route, UriKind.Relative), payload, chunkSize: 64);
        scenario.Scans.ChunksStarted = () => upload.Content.ChunksStarted;

        // Act
        for (var chunk = 1; chunk < upload.Content.ChunkCount; chunk++)
        {
            upload.Content.ReleaseChunk();
            await scenario.Probe.WaitForBytes(upload.Content.BytesAfter(chunk)).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);
            scenario.Scans.Scans.ShouldBeEmpty();
        }

        upload.Content.ReleaseChunk();
        using var result = await upload.Response;

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.Created);
        var scan = scenario.Scans.Scans.ShouldHaveSingleItem();
        scan.Content.ToArray().ShouldBe(MediaUploadKestrelScenario.Png);
        scan.ChunksStarted.ShouldBe(upload.Content.ChunkCount);
        scenario.Probe.LargestRequestedRead.ShouldBeLessThanOrEqualTo(81_920);
        var persistedImages = await scenario.PersistedImageCount(TestContext.Current.CancellationToken);
        persistedImages.ShouldBe(1);
    }

    [Fact]
    public async Task Upload_answers_413_without_scanning_when_the_request_exceeds_the_endpoint_limit()
    {
        // Arrange
        await using var scenario = await MediaUploadKestrelScenario.Start();
        var image = new byte[MediaUploadValidationOptions.DefaultMaxLengthBytes + 16_384 + 4_096];
        MediaUploadKestrelScenario.Png.CopyTo(image, 0);
        var payload = await MediaUploadKestrelScenario.Form(image, TestContext.Current.CancellationToken);
        using var upload = new RawHttpUpload();

        // Act
        await upload.Send(scenario.BaseUri, scenario.Route, scenario.Authorization, payload, bytesToSend: 1_024, TestContext.Current.CancellationToken);
        var statusLine = await upload.ReadStatusLine(TestContext.Current.CancellationToken).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Assert
        statusLine.ShouldBe("HTTP/1.1 413 Payload Too Large");
        scenario.Scans.Scans.ShouldBeEmpty();
        var persistedImages = await scenario.PersistedImageCount(TestContext.Current.CancellationToken);
        persistedImages.ShouldBe(0);
        var storedObjects = await scenario.StoredObjectCount(TestContext.Current.CancellationToken);
        storedObjects.ShouldBe(0);
    }

    [Fact]
    public async Task Upload_answers_400_without_scanning_when_the_file_exceeds_the_file_limit_within_the_request_limit()
    {
        // Arrange
        await using var scenario = await MediaUploadKestrelScenario.Start();
        var image = new byte[MediaUploadValidationOptions.DefaultMaxLengthBytes + 4_096];
        MediaUploadKestrelScenario.Png.CopyTo(image, 0);
        var payload = await MediaUploadKestrelScenario.Form(image, TestContext.Current.CancellationToken);
        using var content = new ReadOnlyMemoryContent(payload.Body);
        content.Headers.TryAddWithoutValidation("Content-Type", payload.ContentType);

        // Act
        using var response = await scenario.Client.PostAsync(new Uri(scenario.Route, UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        scenario.Scans.Scans.ShouldBeEmpty();
        var persistedImages = await scenario.PersistedImageCount(TestContext.Current.CancellationToken);
        persistedImages.ShouldBe(0);
        var storedObjects = await scenario.StoredObjectCount(TestContext.Current.CancellationToken);
        storedObjects.ShouldBe(0);
    }

    [Fact]
    public async Task Upload_answers_408_without_scanning_when_a_stalled_upload_falls_below_the_server_minimum_body_data_rate()
    {
        // Arrange
        await using var scenario = await MediaUploadKestrelScenario.Start(
            host => host.ConfigureServices(services => services.Configure<KestrelServerOptions>(options => options.Limits.MinRequestBodyDataRate = new MinDataRate(bytesPerSecond: 1_000_000, gracePeriod: TimeSpan.FromMilliseconds(1_001)))));
        var payload = await MediaUploadKestrelScenario.Form(MediaUploadKestrelScenario.Png, TestContext.Current.CancellationToken);
        using var upload = new RawHttpUpload();

        // Act
        await upload.Send(scenario.BaseUri, scenario.Route, scenario.Authorization, payload, payload.Body.Length / 2, TestContext.Current.CancellationToken);
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);
        var statusLine = await upload.ReadStatusLine(TestContext.Current.CancellationToken).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Assert
        statusLine.ShouldBe("HTTP/1.1 408 Request Timeout");
        scenario.Scans.Scans.ShouldBeEmpty();
        var persistedImages = await scenario.PersistedImageCount(TestContext.Current.CancellationToken);
        persistedImages.ShouldBe(0);
    }
}
