using System.Text;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PandaBear.MalwareScanning;
using PandaBear.Testing;
using PandaBear.Testing.MalwareScanning;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.ServiceDefaults;
using ViajantesTurismo.UploadWire.Testing;

namespace ViajantesTurismo.Admin.ApiServiceTests.Customers.Import;

/// <summary>
/// Issue 706: the customer import endpoints exercised over real HTTP on a loopback Kestrel server (not TestServer),
/// proving disconnect, slow-client and oversize behavior of the upload intake.
/// </summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
public sealed class CustomerImportUploadOverKestrelTests
{
    [Theory]
    [InlineData(CustomerImportKestrelScenario.ImportRoute, "import", true, "completed")]
    [InlineData(CustomerImportKestrelScenario.CommitRoute, "commit", true, "completed")]
    [InlineData(CustomerImportKestrelScenario.ImportRoute, "import", false, "rejected")]
    [InlineData(CustomerImportKestrelScenario.CommitRoute, "commit", false, "rejected")]
    public async Task Import_records_one_attempt_and_duration_with_only_bounded_dimensions(
        string route,
        string operation,
        bool valid,
        string outcome)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = CustomerImportKestrelScenario.Start(route);
        using var metrics = scenario.RecordMetrics();
        using var form = new MultipartFormDataContent();
        var csv = Encoding.UTF8.GetBytes(valid ? CustomerImportKestrelScenario.ValidCsv : "Unexpected header\nprivate-data@example.test");
        form.Add(MultipartPayload.FilePart(csv, ContractConstants.CustomerImportTextCsvContentType), "file", "untrusted-customer-name.csv");
        KeyValuePair<string, object?>[] tags =
        [
            new("import.operation", operation),
            new("import.outcome", outcome),
            new("import.size_bucket", "up_to_64_kib"),
        ];

        // Act
        using var response = await scenario.Client.PostAsync(new Uri(route, UriKind.Relative), form, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var attempts = metrics.Counters("admin.customer_import.attempts");
        var durations = metrics.Histograms("admin.customer_import.duration");
        attempts.Length.ShouldBe(1);
        attempts[0].Value.ShouldBe(1L);
        attempts[0].Tags.ShouldBe(tags);
        durations.Length.ShouldBe(1);
        var duration = durations[0].Value;
        var finite = double.IsFinite(duration);
        finite.ShouldBeTrue();
        duration.ShouldBeGreaterThanOrEqualTo(0d);
        durations[0].Tags.ShouldBe(tags);
    }

    [Theory]
    [InlineData(65_536, "up_to_64_kib")]
    [InlineData(65_537, "up_to_256_kib")]
    [InlineData(262_144, "up_to_256_kib")]
    [InlineData(262_145, "up_to_1_mib")]
    public async Task Import_records_only_the_size_bucket_when_the_scanner_is_unavailable(int size, string bucket)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string route = CustomerImportKestrelScenario.ImportRoute;
        var scanner = TestMalwareScanner.Throwing(new IOException("Cannot scan traveler@example.test in private.csv"));
        await using var scenario = CustomerImportKestrelScenario.Start(
            route,
            host => host.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<IMalwareScanner>(scanner))));
        using var metrics = scenario.RecordMetrics();
        using var form = new MultipartFormDataContent();
        form.Add(MultipartPayload.FilePart(new byte[size], ContractConstants.CustomerImportTextCsvContentType), "file", "private.csv");
        KeyValuePair<string, object?>[] tags =
        [
            new("import.operation", "import"),
            new("import.outcome", "failed"),
            new("import.size_bucket", bucket),
        ];

        // Act
        using var response = await scenario.Client.PostAsync(new Uri(route, UriKind.Relative), form, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var (count, attemptTags) = metrics.Counters("admin.customer_import.attempts").ShouldHaveSingleItem();
        var (_, durationTags) = metrics.Histograms("admin.customer_import.duration").ShouldHaveSingleItem();
        count.ShouldBe(1L);
        attemptTags.ShouldBe(tags);
        durationTags.ShouldBe(tags);
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Import_records_a_disconnect_during_scanning_as_canceled()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string route = CustomerImportKestrelScenario.ImportRoute;
        var gate = new MalwareScanGate();
        var scanner = TestMalwareScanner.Blocking(gate);
        await using var scenario = CustomerImportKestrelScenario.Start(
            route,
            host => host.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<IMalwareScanner>(scanner))));
        using var metrics = scenario.RecordMetrics();
        var csv = Encoding.UTF8.GetBytes(CustomerImportKestrelScenario.ValidCsv);
        var payload = await CustomerImportKestrelScenario.Form(csv, ct);
        using var upload = new RawHttpUpload();
        await upload.Send(scenario.BaseUri, route, scenario.Authorization, payload, payload.Body.Length, ct);
        await gate.Entered.WaitAsync(WireTimeout.Default, ct);

        // Act
        upload.Abort();
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, ct);

        // Assert
        var (count, tags) = metrics.Counters("admin.customer_import.attempts").ShouldHaveSingleItem();
        count.ShouldBe(1L);
        tags.ShouldContain(new KeyValuePair<string, object?>("import.outcome", "canceled"));
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(CustomerImportKestrelScenario.ImportRoute, false)]
    [InlineData(CustomerImportKestrelScenario.CommitRoute, true)]
    public async Task Import_rejects_an_extra_multipart_section_before_scanning(string route, bool includesResolutions)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = CustomerImportKestrelScenario.Start(route);
        using var form = new MultipartFormDataContent();
        var csv = Encoding.UTF8.GetBytes(CustomerImportKestrelScenario.ValidCsv);
        form.Add(MultipartPayload.FilePart(csv, ContractConstants.CustomerImportTextCsvContentType), "file", "customers.csv");
        if (includesResolutions)
        {
            form.Add(new StringContent("[]"), "conflictResolutions");
        }

        form.Add(new StringContent("Unexpected"), "extra");

        // Act
        using var response = await scenario.Client.PostAsync(new Uri(route, UriKind.Relative), form, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        scenario.ScanProbe.Recorder.CallCount.ShouldBe(0);
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Commit_rejects_conflict_resolutions_over_the_field_budget_before_scanning()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string route = CustomerImportKestrelScenario.CommitRoute;
        await using var scenario = CustomerImportKestrelScenario.Start(route);
        using var form = new MultipartFormDataContent();
        var csv = Encoding.UTF8.GetBytes(CustomerImportKestrelScenario.ValidCsv);
        form.Add(MultipartPayload.FilePart(csv, ContractConstants.CustomerImportTextCsvContentType), "file", "customers.csv");
        form.Add(new StringContent(new string(' ', (int)ContractConstants.CustomerImportConflictResolutionsMaxBytes) + "[]"), "conflictResolutions");

        // Act
        using var response = await scenario.Client.PostAsync(new Uri(route, UriKind.Relative), form, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        scenario.ScanProbe.Recorder.CallCount.ShouldBe(0);
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(CustomerImportKestrelScenario.ImportRoute)]
    [InlineData(CustomerImportKestrelScenario.CommitRoute)]
    public async Task Import_ends_the_request_without_scanning_or_persisting_when_the_client_disconnects_mid_upload(string route)
    {
        // Arrange
        await using var scenario = CustomerImportKestrelScenario.Start(route);
        var csv = Encoding.UTF8.GetBytes(CustomerImportKestrelScenario.ValidCsv + "\n" + new string('x', 65_536));
        var payload = await CustomerImportKestrelScenario.Form(csv, TestContext.Current.CancellationToken);
        var partialLength = payload.Body.Length / 2;
        using var upload = new RawHttpUpload();
        await upload.Send(scenario.BaseUri, route, scenario.Authorization, payload, partialLength, TestContext.Current.CancellationToken);
        await scenario.Probe.WaitForBytes(partialLength).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Act
        upload.Abort();
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Assert
        scenario.Probe.DisconnectObserved.ShouldBeTrue();
        scenario.ScanProbe.Recorder.CallCount.ShouldBe(0);
        scenario.Store.AllCustomers.ShouldBeEmpty();
        scenario.UnitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(CustomerImportKestrelScenario.ImportRoute)]
    [InlineData(CustomerImportKestrelScenario.CommitRoute)]
    public async Task Import_scans_the_exact_bytes_once_after_the_last_chunk_of_a_slow_client(string route)
    {
        // Arrange
        await using var scenario = CustomerImportKestrelScenario.Start(route);
        var csv = Encoding.UTF8.GetBytes(CustomerImportKestrelScenario.ValidCsv);
        var payload = await CustomerImportKestrelScenario.Form(csv, TestContext.Current.CancellationToken);
        using var upload = new SlowUpload(scenario.Client, new Uri(route, UriKind.Relative), payload, chunkSize: 128);
        scenario.ScanProbe.ChunksStarted = () => upload.Content.ChunksStarted;

        // Act
        for (var chunk = 1; chunk < upload.Content.ChunkCount; chunk++)
        {
            upload.Content.ReleaseChunk();
            await scenario.Probe.WaitForBytes(upload.Content.BytesAfter(chunk)).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);
            scenario.ScanProbe.Recorder.CallCount.ShouldBe(0);
        }

        upload.Content.ReleaseChunk();
        using var result = await upload.Response;

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        var scan = scenario.ScanProbe.Recorder.Scans.ShouldHaveSingleItem();
        scan.Content.ToArray().ShouldBe(csv);
        scenario.ScanProbe.ChunksStartedAtScan.ShouldHaveSingleItem().ShouldBe(upload.Content.ChunkCount);
        scenario.Probe.LargestRequestedRead.ShouldBeLessThanOrEqualTo(81_920);
        scenario.Store.AllCustomers.ShouldHaveSingleItem();
    }

    [Fact]
    [Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
    public async Task Rejects_requests_larger_than_the_import_request_limit()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scenario = CustomerImportKestrelScenario.Start(CustomerImportKestrelScenario.ImportRoute);
        using var requestContent = new MultipartFormDataContent();
        using var fileContent = MultipartPayload.FilePart(
            new byte[ContractConstants.CustomerImportMaxRequestBytes + 1], ContractConstants.CustomerImportTextCsvContentType);
        requestContent.Add(fileContent, "file", "customers.csv");

        // Act
        using var response = await scenario.Client.PostAsync(
            new Uri(CustomerImportKestrelScenario.ImportRoute, UriKind.Relative), requestContent, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    [Theory]
    [InlineData(CustomerImportKestrelScenario.ImportRoute)]
    [InlineData(CustomerImportKestrelScenario.CommitRoute)]
    public async Task Import_answers_413_without_scanning_when_the_request_exceeds_the_endpoint_limit(string route)
    {
        // Arrange
        await using var scenario = CustomerImportKestrelScenario.Start(route);
        var csv = new byte[ContractConstants.CustomerImportMaxRequestBytes + 4_096];
        Array.Fill(csv, (byte)'x');
        var payload = await CustomerImportKestrelScenario.Form(csv, TestContext.Current.CancellationToken);
        using var upload = new RawHttpUpload();

        // Act
        await upload.Send(scenario.BaseUri, route, scenario.Authorization, payload, bytesToSend: 1_024, TestContext.Current.CancellationToken);
        var statusLine = await upload.ReadStatusLine(TestContext.Current.CancellationToken).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Assert
        statusLine.ShouldBe("HTTP/1.1 413 Payload Too Large");
        scenario.ScanProbe.Recorder.CallCount.ShouldBe(0);
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(CustomerImportKestrelScenario.ImportRoute)]
    [InlineData(CustomerImportKestrelScenario.CommitRoute)]
    public async Task Import_answers_400_without_scanning_when_the_file_exceeds_the_file_limit_within_the_request_limit(string route)
    {
        // Arrange
        await using var scenario = CustomerImportKestrelScenario.Start(route);
        var csv = new byte[ContractConstants.CustomerImportMaxFileBytes + 4_096];
        Array.Fill(csv, (byte)'x');
        var payload = await CustomerImportKestrelScenario.Form(csv, TestContext.Current.CancellationToken);
        using var content = new ReadOnlyMemoryContent(payload.Body);
        content.Headers.TryAddWithoutValidation("Content-Type", payload.ContentType);

        // Act
        using var response = await scenario.Client.PostAsync(new Uri(route, UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        scenario.ScanProbe.Recorder.CallCount.ShouldBe(0);
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(CustomerImportKestrelScenario.ImportRoute)]
    [InlineData(CustomerImportKestrelScenario.CommitRoute)]
    public async Task Import_answers_408_without_scanning_when_a_stalled_upload_falls_below_the_server_minimum_body_data_rate(string route)
    {
        // Arrange
        await using var scenario = CustomerImportKestrelScenario.Start(route);
        var csv = Encoding.UTF8.GetBytes(CustomerImportKestrelScenario.ValidCsv + "\n" + new string('x', 16_384));
        var payload = await CustomerImportKestrelScenario.Form(csv, TestContext.Current.CancellationToken);
        using var upload = new RawHttpUpload();
        const int prefixLength = 8_192;

        // Act
        await upload.Send(scenario.BaseUri, route, scenario.Authorization, payload, prefixLength, TestContext.Current.CancellationToken);
        var statusLine = await upload.ReadStatusLine(TestContext.Current.CancellationToken).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Assert
        statusLine.ShouldBe("HTTP/1.1 408 Request Timeout");
        scenario.ScanProbe.Recorder.CallCount.ShouldBe(0);
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(CustomerImportKestrelScenario.ImportRoute)]
    [InlineData(CustomerImportKestrelScenario.CommitRoute)]
    public async Task Import_deadline_returns_504_while_the_multipart_body_is_incomplete(string route)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = CustomerImportKestrelScenario.Start(route, host => host.ConfigureServices(services =>
            services.Configure<RequestTimeoutOptions>(options =>
                options.AddPolicy(ApiRequestLimits.MultipartUploadTimeoutPolicy, TimeSpan.FromSeconds(2)))));
        var csv = Encoding.UTF8.GetBytes(CustomerImportKestrelScenario.ValidCsv + "\n" + new string('x', 32_768));
        var payload = await CustomerImportKestrelScenario.Form(csv, ct);
        using var upload = new RawHttpUpload();
        const int prefixLength = 16_384;
        await upload.Send(scenario.BaseUri, route, scenario.Authorization, payload, prefixLength, ct);
        await scenario.Probe.WaitForBytes(prefixLength).WaitAsync(WireTimeout.Default, ct);

        // Act
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
        wait.CancelAfter(TimeSpan.FromSeconds(8));
        var statusLine = await upload.ReadStatusLine(wait.Token);

        // Assert
        statusLine.ShouldBe("HTTP/1.1 504 Gateway Timeout");
        scenario.ScanProbe.Recorder.CallCount.ShouldBe(0);
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }
}
