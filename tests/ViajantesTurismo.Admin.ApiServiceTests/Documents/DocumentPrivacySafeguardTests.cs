using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PandaBear.Testing;
using PandaBear.Idempotency;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing;
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
            DocumentNoStoreHeaders.ArePresent(anonymousResponse).ShouldBeTrue();
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
            DocumentNoStoreHeaders.ArePresent(guestResponse).ShouldBeTrue();
            guestBody.ShouldNotContain(bodyMarker, StringComparison.Ordinal);
            guestBody.ShouldNotContain(tokenMarker, StringComparison.Ordinal);
        }

        using var invalidTokenResponse = await invalidTokenClient.GetAsync(
            new Uri(DocumentEndpointRouteCase.All[1].RequestPath, UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        invalidTokenResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        DocumentNoStoreHeaders.ArePresent(invalidTokenResponse).ShouldBeTrue();
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
    public async Task Authenticated_unmatched_document_path_returns_no_store_headers()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/documents/unmatched", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        DocumentNoStoreHeaders.ArePresent(response).ShouldBeTrue();
    }

    [Fact]
    public async Task Document_no_store_scope_does_not_apply_to_a_sibling_path_prefix()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/documents-public/unmatched", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        DocumentNoStoreHeaders.ArePresent(response).ShouldBeFalse();
    }

    [Fact]
    public async Task Unsupported_document_method_returns_no_store_headers()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            new Uri($"/api/v1/documents/{Guid.CreateVersion7()}", UriKind.Relative));

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        DocumentNoStoreHeaders.ArePresent(response).ShouldBeTrue();
    }

    [Fact]
    public async Task Rate_limited_document_mutation_returns_no_store_headers()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var route = new Uri(
            $"/api/v1/documents/bookings/{Guid.CreateVersion7()}/contract-drafts",
            UriKind.Relative);

        // Act
        for (var requestNumber = 0; requestNumber < 300; requestNumber++)
        {
            using var allowedRequest = new HttpRequestMessage(HttpMethod.Post, route);
            allowedRequest.Headers.TryAddWithoutValidation("Idempotency-Key", "invalid key");
            using var allowedResponse = await client.SendAsync(
                allowedRequest,
                TestContext.Current.CancellationToken);
            allowedResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using var limitedRequest = new HttpRequestMessage(HttpMethod.Post, route);
        limitedRequest.Headers.TryAddWithoutValidation("Idempotency-Key", "invalid key");
        using var limitedResponse = await client.SendAsync(
            limitedRequest,
            TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        DocumentNoStoreHeaders.ArePresent(limitedResponse).ShouldBeTrue();
    }

    [Fact]
    public async Task Unhandled_document_exception_overrides_unsafe_cache_headers_with_no_store()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<GetFinalizedDocumentArtifactHandler>();
            services.Configure<ProblemDetailsOptions>(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.HttpContext.Response.Headers.CacheControl = "public, max-age=3600";
                };
            });
        }, environment: Environments.Production);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/documents/{Guid.CreateVersion7()}/download", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        DocumentNoStoreHeaders.ArePresent(response).ShouldBeTrue();
    }

    [Fact]
    public async Task Document_error_responses_for_every_route_disable_storage()
    {
        // Arrange
        var missingDocuments = new MissingDocumentServices();
        var auditStore = new CapturingDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IIdempotencyStore>();
            services.AddSingleton<IIdempotencyStore, UnstartedDocumentIdempotencyStore>();
            services.RemoveAll<IDocumentStore>();
            services.AddSingleton<IDocumentStore>(missingDocuments);
            services.RemoveAll<IDocumentQueryService>();
            services.AddSingleton<IDocumentQueryService>(missingDocuments);
            services.RemoveAll<IDocumentAuditStore>();
            services.AddSingleton<IDocumentAuditStore>(auditStore);
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton<IUnitOfWork>(unitOfWork);
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var outcomes = new List<(string Pattern, HttpStatusCode StatusCode, bool HasNoStoreHeaders)>();

        // Act
        foreach (var route in DocumentEndpointRouteCase.All)
        {
            using var request = new HttpRequestMessage(
                new HttpMethod(route.Method),
                new Uri(route.RequestPath, UriKind.Relative));
            if (route.Pattern.Contains("bookings/{bookingId:guid}", StringComparison.Ordinal))
            {
                request.Headers.TryAddWithoutValidation("Idempotency-Key", "invalid key");
            }
            else if (route.Pattern.EndsWith("/regenerate", StringComparison.Ordinal))
            {
                request.Headers.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
            }

            if (string.Equals(route.Method, HttpMethod.Patch.Method, StringComparison.Ordinal))
            {
                request.Content = new StringContent("{\"value\":\"Updated\"}", Encoding.UTF8, "application/json");
            }

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            outcomes.Add((route.Pattern, response.StatusCode, DocumentNoStoreHeaders.ArePresent(response)));
        }

        // Assert
        outcomes.Count.ShouldBe(DocumentEndpointRouteCase.All.Count);
        outcomes.ShouldHaveSingleItem(outcome => outcome.Pattern.Contains("bookings/{bookingId:guid}", StringComparison.Ordinal))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        outcomes.Where(outcome => !outcome.Pattern.Contains("bookings/{bookingId:guid}", StringComparison.Ordinal))
            .All(outcome => outcome.StatusCode == HttpStatusCode.NotFound)
            .ShouldBeTrue();
        outcomes.All(outcome => outcome.HasNoStoreHeaders).ShouldBeTrue();
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
