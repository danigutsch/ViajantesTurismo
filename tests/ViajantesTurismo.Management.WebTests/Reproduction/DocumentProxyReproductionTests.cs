using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using System.Text;
using Microsoft.AspNetCore.TestHost;
using SharedKernel.Testing.Snapshots;
using SharedKernel.Testing;
using ViajantesTurismo.Management.WebTests.Components.Pages.Documents;
using ViajantesTurismo.Management.WebTests.Infrastructure;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.Management.WebTests.Reproduction;

/// <summary>Repeats the Management download outcomes through independently created proxy test hosts.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestScope(TestScope.Integration)]
public sealed class DocumentProxyReproductionTests
{
    [Theory]
    [InlineData("Admin", 1, HttpStatusCode.OK, DocumentProxyReproductionScenario.AttachmentDelivered)]
    [InlineData("Admin", 2, HttpStatusCode.OK, DocumentProxyReproductionScenario.AttachmentDelivered)]
    [InlineData("Operator", 1, HttpStatusCode.Forbidden, DocumentProxyReproductionScenario.Forbidden)]
    [InlineData("Operator", 2, HttpStatusCode.Forbidden, DocumentProxyReproductionScenario.Forbidden)]
    public async Task Reproduces_the_download_outcome_and_exports_only_approved_observations(
        string role, int repetition, HttpStatusCode expectedStatus, string outcomeCode)
    {
        // Arrange
        const string headerName = "X-Synthetic-Reproduction-Header";
        const string headerValue = "synthetic-header-marker";
        var html = $"<html>synthetic-document-marker-{repetition}</html>";
        var ct = Xunit.TestContext.Current.CancellationToken;
        using var scratch = new TemporaryDirectory();
        var resultsDirectory = ReproductionFixtureEnvironment.GetLocalResultsDirectory() ?? scratch.Path;
        var specification = DocumentProxyReproductionScenario.Specification;
        var outcome = specification.AllowedOutcomes.ShouldHaveSingleItem(candidate => candidate.OutcomeCode == outcomeCode);
        var documentId = Guid.NewGuid();
        var fileName = $"{documentId:N}.html";
        var requestedDocuments = new List<Guid>();
        var documents = new FakeDocumentsApiClient
        {
            DownloadArtifactHandler = (id, _) =>
            {
                requestedDocuments.Add(id);
                return Task.FromResult<DocumentArtifactResponse?>(new DocumentArtifactResponse(Encoding.UTF8.GetBytes(html), fileName));
            }
        };

        using var activity = new Activity(specification.Scenario);
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        activity.Start();
        var traceparent = activity.Id.ShouldNotBeNull();
        var trace = new DocumentProxyTraceObservation(activity.TraceId);
        var startedUtc = TimeProvider.System.GetUtcNow();
        var started = Stopwatch.GetTimestamp();
        var attemptId = Guid.NewGuid();
        using var host = await ManagementWebEndpointTestHost.StartWithRecordingAuthentication(
            ct, documentsApiClient: documents);
        using var traceListener = trace.Listen(host);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/documents/{documentId}/download", UriKind.Relative));
        request.Headers.Add("X-Test-Role", role);
        request.Headers.Add(headerName, headerValue);
        request.Headers.Add("traceparent", traceparent);

        // Act
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
        if (expectedStatus == HttpStatusCode.OK)
        {
            requestedDocuments.ShouldHaveSingleItem().ShouldBe(documentId);
            response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
            response.Headers.Pragma.ShouldContain(value => string.Equals(value.Name, "no-cache", StringComparison.OrdinalIgnoreCase));
            response.Content.Headers.GetValues("Expires").ShouldHaveSingleItem().ShouldBe("Thu, 01 Jan 1970 00:00:00 GMT");
            response.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
            response.Headers.GetValues("Content-Security-Policy").ShouldHaveSingleItem().ShouldBe("default-src 'none'; sandbox");
            response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/html");
            var attachment = response.Content.Headers.ContentDisposition.ShouldNotBeNull();
            attachment.DispositionType.ShouldBe("attachment");
            attachment.FileNameStar.ShouldBe(fileName);

            body.ShouldBe(html);
        }
        else
        {
            requestedDocuments.ShouldBeEmpty();
            response.Content.Headers.ContentDisposition.ShouldBeNull();
            body.ShouldNotContain("synthetic-document-marker", StringComparison.Ordinal);
        }

        var observed = trace.ObservedTrace.ShouldNotBeNull();
        observed.ShouldBe(activity.TraceId);
        var manifest = new ReproductionManifest(specification.Revision, specification.Scenario, attemptId, [observed], outcome,
            startedUtc, checked((int)Stopwatch.GetElapsedTime(started).TotalMilliseconds));
        var path = await ReproductionManifestCodec.Write(resultsDirectory, specification, manifest, ct);
        var persisted = await ReproductionManifestCodec.Read(path, specification, ct);
        var artifactJson = await File.ReadAllTextAsync(path, ct);

        persisted.AttemptId.ShouldBe(attemptId);
        persisted.TraceIds.ShouldHaveSingleItem().ShouldBe(observed);
        persisted.Outcome.OutcomeCode.ShouldBe(outcomeCode);
        persisted.Outcome.ResourceStates.Keys.ShouldHaveSingleItem().ShouldBe("management-web");
        artifactJson.ShouldNotContain(html, StringComparison.Ordinal);
        artifactJson.ShouldNotContain("synthetic-document-marker", StringComparison.Ordinal);
        artifactJson.ShouldNotContain(fileName, StringComparison.Ordinal);
        artifactJson.ShouldNotContain(documentId.ToString("D"), StringComparison.Ordinal);
        artifactJson.ShouldNotContain(headerName, StringComparison.Ordinal);
        artifactJson.ShouldNotContain(headerValue, StringComparison.Ordinal);
        artifactJson.ShouldNotContain(traceparent, StringComparison.Ordinal);
        artifactJson.ShouldNotContain("Content-Disposition", StringComparison.OrdinalIgnoreCase);

        foreach (var (field, value) in new[] { ("headers", headerValue), ("body", html) })
        {
            // Act
            var json = JsonNode.Parse(artifactJson).ShouldNotBeNull().AsObject();
            json.Add(field, value);
            var contaminated = Path.Combine(scratch.Path, $"{field}.json");
            await File.WriteAllTextAsync(contaminated, json.ToJsonString(), ct);
            Func<Task> readContaminated = async () => _ = await ReproductionManifestCodec.Read(contaminated, specification, ct);

            // Assert
            var rejected = await readContaminated.ShouldThrow<InvalidDataException>();
            rejected.Message.ShouldBe("The reproduction manifest does not match the approved fixture.");
        }
    }
}
