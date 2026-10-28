using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.UploadWire.Testing;

namespace ViajantesTurismo.Admin.ApiServiceTests;

/// <summary>
/// Issue 706: the customer import endpoints exercised over real HTTP on a loopback Kestrel server (not TestServer),
/// proving disconnect, slow-client and oversize behavior of the upload intake.
/// </summary>
public sealed class CustomerImportUploadOverKestrelTests
{
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
        scenario.Scans.Scans.ShouldBeEmpty();
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
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        var scan = scenario.Scans.Scans.ShouldHaveSingleItem();
        scan.Content.ToArray().ShouldBe(csv);
        scan.ChunksStarted.ShouldBe(upload.Content.ChunkCount);
        scenario.Probe.LargestRequestedRead.ShouldBeLessThanOrEqualTo(81_920);
        scenario.Store.AllCustomers.ShouldHaveSingleItem();
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
        scenario.Scans.Scans.ShouldBeEmpty();
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
        scenario.Scans.Scans.ShouldBeEmpty();
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(CustomerImportKestrelScenario.ImportRoute)]
    [InlineData(CustomerImportKestrelScenario.CommitRoute)]
    public async Task Import_answers_408_without_scanning_when_a_stalled_upload_falls_below_the_server_minimum_body_data_rate(string route)
    {
        // Arrange
        await using var scenario = CustomerImportKestrelScenario.Start(
            route,
            host => host.ConfigureServices(services => services.Configure<KestrelServerOptions>(options => options.Limits.MinRequestBodyDataRate = new MinDataRate(bytesPerSecond: 1_000_000, gracePeriod: TimeSpan.FromMilliseconds(1_001)))));
        var csv = Encoding.UTF8.GetBytes(CustomerImportKestrelScenario.ValidCsv);
        var payload = await CustomerImportKestrelScenario.Form(csv, TestContext.Current.CancellationToken);
        using var upload = new RawHttpUpload();

        // Act
        await upload.Send(scenario.BaseUri, route, scenario.Authorization, payload, payload.Body.Length / 2, TestContext.Current.CancellationToken);
        await scenario.Probe.Finished.WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);
        var statusLine = await upload.ReadStatusLine(TestContext.Current.CancellationToken).WaitAsync(WireTimeout.Default, TestContext.Current.CancellationToken);

        // Assert
        statusLine.ShouldBe("HTTP/1.1 408 Request Timeout");
        scenario.Scans.Scans.ShouldBeEmpty();
        scenario.Store.AllCustomers.ShouldBeEmpty();
    }
}
