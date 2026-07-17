using System.Net;
using Microsoft.AspNetCore.TestHost;
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
}
