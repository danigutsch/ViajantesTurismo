using Microsoft.AspNetCore.Http.Timeouts;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.ServiceDefaults;
using ViajantesTurismo.UploadWire.Testing;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Media;

/// <summary>
/// Issue 706: the Catalog tour image upload exercised over real HTTP on a loopback Kestrel server (not TestServer),
/// proving disconnect, slow-client and oversize behavior of the upload intake.
/// </summary>
[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.AreaName, TestTraits.CatalogArea)]
public sealed class MediaUploadOverKestrelTests
{
    [Fact]
    public async Task JSON_mutation_rejects_a_request_above_the_host_backstop()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await MediaUploadKestrelScenario.Start();
        var body = new byte[8 * 1024 * 1024 + 1];
        Array.Fill(body, (byte)' ');
        "{}"u8.CopyTo(body);
        var route = new Uri($"/api/v1/catalog/tours/{scenario.TourId}/presentation", UriKind.Relative);
        using var request = new HttpRequestMessage(HttpMethod.Put, route)
        {
            Content = new ReadOnlyMemoryContent(body)
        };
        request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
        request.Headers.ExpectContinue = true;

        // Act
        using var response = await scenario.Client.SendAsync(request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Upload_rejects_more_than_five_multipart_sections_before_scanning()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await MediaUploadKestrelScenario.Start();
        using var form = new MultipartFormDataContent();
        form.Add(MultipartPayload.FilePart(MediaUploadKestrelScenario.Png, "image/png"), "file", "tour.png");
        form.Add(new StringContent("Sunset over the mountains"), "altText");
        form.Add(new StringContent("Mountain view"), "caption");
        form.Add(new StringContent("Photographer"), "attribution");
        form.Add(new StringContent("Copyright"), "copyright");
        form.Add(new StringContent("Unexpected"), "extra");

        // Act
        using var response = await scenario.Client.PostAsync(new Uri(scenario.Route, UriKind.Relative), form, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        scenario.Scans.Scans.ShouldBeEmpty();
        var persistedImages = await scenario.PersistedImageCount(ct);
        persistedImages.ShouldBe(0);
    }

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
        await using var scenario = await MediaUploadKestrelScenario.Start();
        var image = new byte[16_384];
        MediaUploadKestrelScenario.Png.CopyTo(image, 0);
        var payload = await MediaUploadKestrelScenario.Form(image, TestContext.Current.CancellationToken);
        using var upload = new RawHttpUpload();
        const int prefixLength = 8_192;

        // Act
        await upload.Send(scenario.BaseUri, scenario.Route, scenario.Authorization, payload, prefixLength, TestContext.Current.CancellationToken);
        var statusLine = await upload.ReadStatusLine(TestContext.Current.CancellationToken).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Assert
        statusLine.ShouldBe("HTTP/1.1 408 Request Timeout");
        scenario.Scans.Scans.ShouldBeEmpty();
        var persistedImages = await scenario.PersistedImageCount(TestContext.Current.CancellationToken);
        persistedImages.ShouldBe(0);
    }

    [Fact]
    public async Task Upload_deadline_returns_504_while_the_multipart_body_is_incomplete()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await MediaUploadKestrelScenario.Start(host => host.ConfigureServices(services =>
            services.Configure<RequestTimeoutOptions>(options =>
                options.AddPolicy(ApiRequestLimits.MultipartUploadTimeoutPolicy, TimeSpan.FromSeconds(2)))));
        var payload = await MediaUploadKestrelScenario.Form(new byte[32_768], ct);
        using var upload = new RawHttpUpload();
        const int prefixLength = 16_384;
        await upload.Send(scenario.BaseUri, scenario.Route, scenario.Authorization, payload, prefixLength, ct);
        await scenario.Probe.WaitForBytes(prefixLength).WaitAsync(WireTimeout.Default, ct);

        // Act
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
        wait.CancelAfter(TimeSpan.FromSeconds(8));
        var statusLine = await upload.ReadStatusLine(wait.Token);

        // Assert
        statusLine.ShouldBe("HTTP/1.1 504 Gateway Timeout");
        scenario.Scans.Scans.ShouldBeEmpty();
        var persistedImages = await scenario.PersistedImageCount(ct);
        persistedImages.ShouldBe(0);
    }
}
