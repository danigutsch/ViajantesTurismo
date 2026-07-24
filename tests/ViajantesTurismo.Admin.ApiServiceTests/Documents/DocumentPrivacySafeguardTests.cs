using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class DocumentPrivacySafeguardTests
{
    [Fact]
    public async Task Authorization_cases_cover_every_mapped_document_route_and_policy()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        var expected = DocumentEndpointRouteCase.All
            .Select(route => (route.Method, route.Pattern))
            .OrderBy(route => route.Pattern, StringComparer.Ordinal)
            .ThenBy(route => route.Method, StringComparer.Ordinal)
            .ToArray();

        // Act
        var endpoints = factory.Services
            .GetServices<EndpointDataSource>()
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?
                .TrimStart('/')
                .StartsWith("api/v1/documents", StringComparison.Ordinal) is true)
            .ToArray();
        var actual = endpoints
            .SelectMany(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                .Select(method => (method, endpoint.RoutePattern.RawText?.TrimStart('/') ?? string.Empty))
                ?? [])
            .OrderBy(route => route.Item2, StringComparer.Ordinal)
            .ThenBy(route => route.method, StringComparer.Ordinal)
            .ToArray();

        // Assert
        actual.ShouldBe(expected);
        foreach (var endpoint in endpoints)
        {
            endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Select(metadata => metadata.Policy)
                .ShouldContain(AdminAuthorization.DocumentManage);
        }
    }

    [Fact]
    public async Task Authorization_failures_do_not_execute_audits_or_expose_request_and_token_markers()
    {
        // Arrange
        const string bodyMarker = "SYNTHETIC_RAW_DOCUMENT_REQUEST_7F31";
        const string tokenMarker = "SYNTHETIC_BEARER_TOKEN_8A42";
        var auditStore = new CapturingDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        using var loggerProvider = new DocumentPrivacyLoggerProvider();
        using var telemetryCapture = new DocumentPrivacyTelemetryCapture();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IDocumentAuditStore>();
            services.AddSingleton<IDocumentAuditStore>(auditStore);
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton<IUnitOfWork>(unitOfWork);
            services.AddLogging(logging => logging.AddProvider(loggerProvider));
        });
        using var anonymousClient = factory.CreateClient();
        using var guestClient = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(guestClient, "Guest");
        using var invalidTokenClient = factory.CreateClient();
        invalidTokenClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenMarker);
        using var parentActivity = telemetryCapture.StartParent();

        // Act
        foreach (var route in DocumentEndpointRouteCase.All)
        {
            using var anonymousRequest = new HttpRequestMessage(new HttpMethod(route.Method), new Uri(route.RequestPath, UriKind.Relative));
            anonymousRequest.Headers.TryAddWithoutValidation("X-Synthetic-Authorization-Marker", tokenMarker);
            if (string.Equals(route.Method, HttpMethod.Patch.Method, StringComparison.Ordinal))
            {
                anonymousRequest.Content = new StringContent($$"""{"value":"{{bodyMarker}}"}""", Encoding.UTF8, "application/json");
            }

            using var anonymousResponse = await anonymousClient.SendAsync(anonymousRequest, TestContext.Current.CancellationToken);
            var anonymousBody = await anonymousResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            anonymousResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            anonymousBody.ShouldNotContain(bodyMarker, StringComparison.Ordinal);
            anonymousBody.ShouldNotContain(tokenMarker, StringComparison.Ordinal);

            using var guestRequest = new HttpRequestMessage(new HttpMethod(route.Method), new Uri(route.RequestPath, UriKind.Relative));
            guestRequest.Headers.TryAddWithoutValidation("X-Synthetic-Authorization-Marker", tokenMarker);
            if (string.Equals(route.Method, HttpMethod.Patch.Method, StringComparison.Ordinal))
            {
                guestRequest.Content = new StringContent($$"""{"value":"{{bodyMarker}}"}""", Encoding.UTF8, "application/json");
            }

            using var guestResponse = await guestClient.SendAsync(guestRequest, TestContext.Current.CancellationToken);
            var guestBody = await guestResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            guestResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            guestBody.ShouldNotContain(bodyMarker, StringComparison.Ordinal);
            guestBody.ShouldNotContain(tokenMarker, StringComparison.Ordinal);
        }

        using var invalidTokenResponse = await invalidTokenClient.GetAsync(
            new Uri(DocumentEndpointRouteCase.All[1].RequestPath, UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        invalidTokenResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        var logs = string.Join('\n', loggerProvider.Messages);
        logs.ShouldNotContain(bodyMarker, StringComparison.Ordinal);
        logs.ShouldNotContain(tokenMarker, StringComparison.Ordinal);
        var telemetry = telemetryCapture.GetCapturedText(parentActivity.TraceId);
        telemetry.ShouldNotContain(bodyMarker, StringComparison.Ordinal);
        telemetry.ShouldNotContain(tokenMarker, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejected_document_audit_logs_and_telemetry_exclude_content_and_request_markers()
    {
        // Arrange
        const string contentMarker = "SYNTHETIC_DOCUMENT_CONTENT_9B53";
        const string requestMarker = "SYNTHETIC_RAW_REQUEST_BODY_A164";
        const string tokenMarker = "SYNTHETIC_AUTHORIZATION_TOKEN_B275";
        var missingDocuments = new MissingDocumentServices();
        var auditStore = new CapturingDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        using var loggerProvider = new DocumentPrivacyLoggerProvider();
        using var telemetryCapture = new DocumentPrivacyTelemetryCapture();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IDocumentStore>();
            services.AddSingleton<IDocumentStore>(missingDocuments);
            services.RemoveAll<IDocumentQueryService>();
            services.AddSingleton<IDocumentQueryService>(missingDocuments);
            services.RemoveAll<IDocumentAuditStore>();
            services.AddSingleton<IDocumentAuditStore>(auditStore);
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton<IUnitOfWork>(unitOfWork);
            services.AddLogging(logging => logging.AddProvider(loggerProvider));
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var documentId = Guid.CreateVersion7();
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            new Uri($"/api/v1/documents/{documentId}/fields/greeting", UriKind.Relative));
        request.Headers.TryAddWithoutValidation("X-Synthetic-Authorization-Marker", tokenMarker);
        request.Content = new StringContent(
            $$"""
            {
              "value": "{{contentMarker}}",
              "rawRequest": "{{requestMarker}}"
            }
            """,
            Encoding.UTF8,
            "application/json");
        using var parentActivity = telemetryCapture.StartParent();

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var auditJson = JsonSerializer.Serialize(auditStore.Records);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.UpdateField);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        foreach (var marker in new[] { contentMarker, requestMarker, tokenMarker })
        {
            responseBody.ShouldNotContain(marker, StringComparison.Ordinal);
            auditJson.ShouldNotContain(marker, StringComparison.Ordinal);
            string.Join('\n', loggerProvider.Messages).ShouldNotContain(marker, StringComparison.Ordinal);
            telemetryCapture.GetCapturedText(parentActivity.TraceId).ShouldNotContain(marker, StringComparison.Ordinal);
        }
    }
}
