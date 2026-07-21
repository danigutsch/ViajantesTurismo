using System.Net;
using Microsoft.AspNetCore.TestHost;
using Polly.Timeout;
using SharedKernel.Testing;
using ViajantesTurismo.Management.WebTests.Components.Pages.Documents;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernelTestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class DocumentArtifactProxyEndpointTests
{
    [Fact]
    public async Task Document_artifact_proxy_relays_html_with_private_delivery_headers_for_admin()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var documentsApiClient = new FakeDocumentsApiClient
        {
            Artifact = new DocumentArtifactResponse("<html>contract</html>"u8.ToArray(), $"{documentId:N}.html")
        };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            Xunit.TestContext.Current.CancellationToken,
            documentsApiClient: documentsApiClient);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/documents/{documentId}/download", UriKind.Relative),
            Xunit.TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.NoStore.ShouldBeTrue();
        response.Headers.Pragma.ShouldContain(header => string.Equals(header.Name, "no-cache", StringComparison.OrdinalIgnoreCase));
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        response.Content.Headers.ContentDisposition?.FileNameStar.ShouldBe($"{documentId:N}.html");
        html.ShouldBe("<html>contract</html>");
    }

    [Fact]
    public async Task Document_artifact_proxy_rejects_authenticated_non_admin_callers()
    {
        // Arrange
        var documentsApiClient = new FakeDocumentsApiClient
        {
            Artifact = new DocumentArtifactResponse("<html>contract</html>"u8.ToArray(), "document.html")
        };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            Xunit.TestContext.Current.CancellationToken,
            documentsApiClient: documentsApiClient);
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Operator");

        // Act
        using var response = await client.GetAsync(
            new Uri($"/documents/{Guid.CreateVersion7()}/download", UriKind.Relative),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Document_artifact_proxy_preserves_downstream_conflict()
    {
        // Arrange
        var documentsApiClient = new FakeDocumentsApiClient
        {
            DownloadArtifactHandler = (_, _) => Task.FromException<DocumentArtifactResponse?>(
                new HttpRequestException("Artifact unavailable.", null, HttpStatusCode.Conflict))
        };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            Xunit.TestContext.Current.CancellationToken,
            documentsApiClient: documentsApiClient);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/documents/{Guid.CreateVersion7()}/download", UriKind.Relative),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Document_artifact_proxy_maps_upstream_timeout_to_gateway_timeout()
    {
        // Arrange
        var documentsApiClient = new FakeDocumentsApiClient
        {
            DownloadArtifactHandler = (_, _) => Task.FromException<DocumentArtifactResponse?>(
                new TimeoutRejectedException())
        };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            Xunit.TestContext.Current.CancellationToken,
            documentsApiClient: documentsApiClient);
        using var client = host.GetTestClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/documents/{Guid.CreateVersion7()}/download", UriKind.Relative),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
    }

    [Fact]
    public async Task Document_artifact_proxy_maps_upstream_cancellation_without_swallowing_request_cancellation()
    {
        // Arrange
        var documentsApiClient = new FakeDocumentsApiClient
        {
            DownloadArtifactHandler = (_, _) => Task.FromException<DocumentArtifactResponse?>(
                new OperationCanceledException("Upstream request timed out."))
        };
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            Xunit.TestContext.Current.CancellationToken,
            documentsApiClient: documentsApiClient);
        using var client = host.GetTestClient();

        // Act
        using var timeoutResponse = await client.GetAsync(
            new Uri($"/documents/{Guid.CreateVersion7()}/download", UriKind.Relative),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        timeoutResponse.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);

        // Arrange
        var requestEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        documentsApiClient.DownloadArtifactHandler = async (_, ct) =>
        {
            requestEntered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return null;
        };
        using var requestCancellation = new CancellationTokenSource();

        // Act
        var request = client.GetAsync(
            new Uri($"/documents/{Guid.CreateVersion7()}/download", UriKind.Relative),
            requestCancellation.Token);
        await requestEntered.Task.WaitAsync(Xunit.TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();
        Func<Task> awaitRequest = async () => _ = await request;

        // Assert
        _ = await awaitRequest.ShouldThrow<TaskCanceledException>();
    }
}
