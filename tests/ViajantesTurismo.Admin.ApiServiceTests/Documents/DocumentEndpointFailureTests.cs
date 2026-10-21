using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PandaBear.Testing;
using PandaBear.Idempotency;
using ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentEndpointFailureTests
{
    private const string OversizedIdempotencyKey =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Theory]
    [InlineData("generate", null, false)]
    [InlineData("generate", "", false)]
    [InlineData("generate", " ", false)]
    [InlineData("generate", "invalid key", false)]
    [InlineData("generate", OversizedIdempotencyKey, false)]
    [InlineData("generate", "first-key", true)]
    [InlineData("regenerate", null, false)]
    [InlineData("regenerate", "", false)]
    [InlineData("regenerate", " ", false)]
    [InlineData("regenerate", "invalid key", false)]
    [InlineData("regenerate", OversizedIdempotencyKey, false)]
    [InlineData("regenerate", "first-key", true)]
    public async Task Invalid_idempotency_headers_are_rejected_before_document_commands_run(string operation, string? key, bool multipleValues)
    {
        // Arrange
        var resourceId = Guid.CreateVersion7();
        var route = operation switch
        {
            "generate" => $"/api/v1/documents/bookings/{resourceId}/contract-drafts",
            "regenerate" => $"/api/v1/documents/{resourceId}/regenerate",
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported document operation.")
        };
        var missingDocuments = new MissingDocumentServices();
        var auditStore = new CapturingDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var idempotencyStore = new UnstartedDocumentIdempotencyStore();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IQueryService>();
            services.AddSingleton<IQueryService, MissingDocumentSourceQueries>();
            services.RemoveAll<IIdempotencyStore>();
            services.AddSingleton<IIdempotencyStore>(idempotencyStore);
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
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(route, UriKind.Relative));
        if (key is not null)
        {
            string[] headerValues = multipleValues ? [key, "second-key"] : [key];
            request.Headers.TryAddWithoutValidation("Idempotency-Key", headerValues).ShouldBeTrue();
        }

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        DocumentNoStoreHeaders.ArePresent(response).ShouldBeTrue();
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe((int?)HttpStatusCode.BadRequest);
        problem.Errors.ContainsKey("Idempotency-Key").ShouldBeTrue();
        var expectedMessage = string.IsNullOrEmpty(key) ? "Idempotency-Key is required." : "Idempotency-Key is invalid.";
        problem.Errors["Idempotency-Key"].ShouldHaveSingleItem().ShouldBe(expectedMessage);
        idempotencyStore.RequestedOperations.ShouldBeEmpty();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("generate")]
    [InlineData("regenerate")]
    public async Task A_valid_padded_idempotency_key_reaches_the_command_with_its_normalized_value(string operation)
    {
        // Arrange
        var resourceId = Guid.CreateVersion7();
        var route = operation == "generate"
            ? $"/api/v1/documents/bookings/{resourceId}/contract-drafts"
            : $"/api/v1/documents/{resourceId}/regenerate";
        var missingDocuments = new MissingDocumentServices();
        var auditStore = new CapturingDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var idempotencyStore = new UnstartedDocumentIdempotencyStore();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IQueryService>();
            services.AddSingleton<IQueryService, MissingDocumentSourceQueries>();
            services.RemoveAll<IIdempotencyStore>();
            services.AddSingleton<IIdempotencyStore>(idempotencyStore);
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
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(route, UriKind.Relative));
        request.Headers.TryAddWithoutValidation("Idempotency-Key", "  valid-key  ").ShouldBeTrue();

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        DocumentNoStoreHeaders.ArePresent(response).ShouldBeTrue();
        var requestedOperation = idempotencyStore.RequestedOperations.ShouldHaveSingleItem();
        requestedOperation.Key.Value.ShouldBe("valid-key");
        auditStore.Records.ShouldHaveSingleItem().Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("GET", "", DocumentAuditOperation.Read)]
    [InlineData("POST", "/review", DocumentAuditOperation.BeginReview)]
    [InlineData("POST", "/changes-requested", DocumentAuditOperation.RequestChanges)]
    [InlineData("PATCH", "/fields/greeting", DocumentAuditOperation.UpdateField)]
    [InlineData("POST", "/approve", DocumentAuditOperation.Approve)]
    [InlineData("POST", "/finalize", DocumentAuditOperation.Finalize)]
    [InlineData("POST", "/regenerate", DocumentAuditOperation.Regenerate)]
    [InlineData("POST", "/void", DocumentAuditOperation.Void)]
    [InlineData("GET", "/download", DocumentAuditOperation.Download)]
    public async Task Missing_document_operations_return_not_found_and_persist_rejected_audits(
        string method,
        string suffix,
        DocumentAuditOperation expectedOperation)
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
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
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            new Uri($"/api/v1/documents/{documentId}{suffix}", UriKind.Relative));
        if (string.Equals(suffix, "/regenerate", StringComparison.Ordinal))
        {
            request.Headers.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        }
        if (string.Equals(method, "PATCH", StringComparison.Ordinal))
        {
            request.Content = JsonContent.Create(new UpdateDocumentFieldDto { Value = "Updated" });
        }

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(expectedOperation);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBe(documentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe("test-user");
        string.IsNullOrWhiteSpace(audit.CorrelationId).ShouldBeFalse();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(
        "/review",
        DocumentAuditOperation.BeginReview,
        nameof(DbUpdateConcurrencyException),
        "The document was changed by another request. Reload and retry.")]
    [InlineData(
        "/regenerate",
        DocumentAuditOperation.Regenerate,
        nameof(DbUpdateConcurrencyException),
        "The document was changed by another request. Reload and retry.")]
    [InlineData(
        "/review",
        DocumentAuditOperation.BeginReview,
        nameof(DocumentRevisionConflictException),
        "A document revision already exists for this booking. Reload and retry.")]
    [InlineData(
        "/regenerate",
        DocumentAuditOperation.Regenerate,
        nameof(DocumentRevisionConflictException),
        "A document revision already exists for this booking. Reload and retry.")]
    [InlineData(
        "/review",
        DocumentAuditOperation.BeginReview,
        nameof(DocumentBookingEligibilityConflictException),
        "A customer-facing document draft requires a confirmed or completed booking.")]
    [InlineData(
        "/regenerate",
        DocumentAuditOperation.Regenerate,
        nameof(DocumentBookingEligibilityConflictException),
        "A customer-facing document draft requires a confirmed or completed booking.")]
    public async Task Document_command_exception_families_return_conflict_with_the_required_audit_behavior(
        string suffix,
        DocumentAuditOperation expectedOperation,
        string exceptionFamily,
        string expectedDetail)
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        Exception exception = exceptionFamily switch
        {
            nameof(DbUpdateConcurrencyException) => new DbUpdateConcurrencyException(),
            nameof(DocumentRevisionConflictException) => new DocumentRevisionConflictException(),
            nameof(DocumentBookingEligibilityConflictException) => new DocumentBookingEligibilityConflictException(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(exceptionFamily),
                exceptionFamily,
                "Unsupported document exception family.")
        };
        var queryService = new MissingDocumentServices();
        var auditStore = new CapturingDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IIdempotencyStore>();
            services.AddSingleton<IIdempotencyStore, UnstartedDocumentIdempotencyStore>();
            services.RemoveAll<IDocumentStore>();
            services.AddSingleton<IDocumentStore>(new ThrowingDocumentStore(exception));
            services.RemoveAll<IDocumentQueryService>();
            services.AddSingleton<IDocumentQueryService>(queryService);
            services.RemoveAll<IDocumentAuditStore>();
            services.AddSingleton<IDocumentAuditStore>(auditStore);
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton<IUnitOfWork>(unitOfWork);
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"/api/v1/documents/{documentId}{suffix}", UriKind.Relative));
        if (string.Equals(suffix, "/regenerate", StringComparison.Ordinal))
        {
            request.Headers.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        }

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe((int?)HttpStatusCode.Conflict);
        problem.Title.ShouldBe("Conflict");
        problem.Detail.ShouldBe(expectedDetail);

        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(expectedOperation);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBe(documentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe("test-user");
        string.IsNullOrWhiteSpace(audit.CorrelationId).ShouldBeFalse();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
