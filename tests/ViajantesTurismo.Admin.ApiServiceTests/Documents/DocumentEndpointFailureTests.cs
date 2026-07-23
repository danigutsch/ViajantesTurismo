using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.Testing;
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
        "The document was changed by another request. Reload and retry.",
        true)]
    [InlineData(
        "/regenerate",
        DocumentAuditOperation.Regenerate,
        nameof(DbUpdateConcurrencyException),
        "The document was changed by another request. Reload and retry.",
        false)]
    [InlineData(
        "/review",
        DocumentAuditOperation.BeginReview,
        nameof(DocumentRevisionConflictException),
        "A document revision already exists for this booking. Reload and retry.",
        true)]
    [InlineData(
        "/regenerate",
        DocumentAuditOperation.Regenerate,
        nameof(DocumentRevisionConflictException),
        "A document revision already exists for this booking. Reload and retry.",
        false)]
    [InlineData(
        "/review",
        DocumentAuditOperation.BeginReview,
        nameof(DocumentBookingEligibilityConflictException),
        "A customer-facing document draft requires a confirmed or completed booking.",
        true)]
    [InlineData(
        "/regenerate",
        DocumentAuditOperation.Regenerate,
        nameof(DocumentBookingEligibilityConflictException),
        "A customer-facing document draft requires a confirmed or completed booking.",
        true)]
    public async Task Document_command_exception_families_return_conflict_with_the_required_audit_behavior(
        string suffix,
        DocumentAuditOperation expectedOperation,
        string exceptionFamily,
        string expectedDetail,
        bool shouldPersistRejectedAudit)
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

        if (!shouldPersistRejectedAudit)
        {
            auditStore.Records.ShouldBeEmpty();
            unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
            return;
        }

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
