using System.Text.Json;
using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(TestTraitNames.CapabilityName, ViajantesTurismo.Admin.Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class DocumentStaffMutationTransactionPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task An_unknown_field_records_rejection_metadata_without_the_field_or_its_value()
    {
        // Arrange
        const string field = "synthetic-traveler@example.test";
        const string value = "SYNTHETIC_DOCUMENT_MEDICAL_NOTE";
        var ct = TestContext.Current.CancellationToken;
        var document = DocumentDraftInfrastructureTestData.CreateDraft(TimeProvider.System.GetUtcNow().UtcDateTime);
        var context = DocumentDraftInfrastructureTestData.CreateAuditContext();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(document);

        // Act
        var result = await scenario.UpdateField(new UpdateDocumentFieldCommand(document.Id, field, value, context), ct);
        var audits = await scenario.GetDocumentAuditsForCorrelation(context.CorrelationId, ct);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        var rejection = audits.ShouldHaveSingleItem();
        rejection.DocumentId.ShouldBe(document.Id);
        rejection.Operation.ShouldBe(DocumentAuditOperation.UpdateField);
        rejection.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        rejection.ReasonCode.ShouldBe(DocumentAuditReasonCode.ValidationRejected);
        var metadata = JsonSerializer.Serialize(rejection);
        metadata.ShouldNotContain(field, StringComparison.Ordinal);
        metadata.ShouldNotContain(value, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DocumentAuditOperation.UpdateField, ResultStatus.Conflict, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.Approve, ResultStatus.Conflict, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.Void, ResultStatus.Invalid, DocumentAuditReasonCode.ValidationRejected)]
    public async Task Rejected_document_mutation_preserves_the_document_and_records_the_rejection(
        DocumentAuditOperation operation,
        ResultStatus expectedStatus,
        DocumentAuditReasonCode expectedReasonCode)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var document = DocumentDraftInfrastructureTestData.CreateFinalizedDraft(TimeProvider.System.GetUtcNow().UtcDateTime);
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(document);

        // Act
        var result = await scenario.MutateDocument(operation, document.Id, auditContext, (DocumentVoidReason)99, ct);

        // Assert
        result.Status.ShouldBe(expectedStatus);
        var persisted = await scenario.GetDocumentById(document.Id, ct);
        persisted.ShouldNotBeNull().Status.ShouldBe(DocumentStatus.Finalized);
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        var rejection = audits.ShouldHaveSingleItem();
        rejection.Operation.ShouldBe(operation);
        rejection.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        rejection.ReasonCode.ShouldBe(expectedReasonCode);
        rejection.DocumentId.ShouldBe(document.Id);
        rejection.BookingId.ShouldBe(document.BookingId);
        rejection.DocumentRevision.ShouldBe(document.Revision);
        JsonSerializer.Serialize(rejection).ShouldNotContain(PostgreSqlDocumentStoreScenario.StaffOverride, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DocumentAuditOperation.UpdateField)]
    [InlineData(DocumentAuditOperation.Approve)]
    [InlineData(DocumentAuditOperation.Void)]
    public async Task Missing_document_persists_one_redacted_staff_mutation_rejection(DocumentAuditOperation operation)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existing = DocumentDraftInfrastructureTestData.CreateDraft(TimeProvider.System.GetUtcNow().UtcDateTime);
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        var missingId = Guid.CreateVersion7();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(existing);

        // Act
        var result = await scenario.MutateDocument(operation, missingId, auditContext, DocumentVoidReason.AdministrativeVoid, ct);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        var rejection = audits.ShouldHaveSingleItem();
        rejection.Operation.ShouldBe(operation);
        rejection.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        rejection.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        rejection.ActorId.ShouldBe(auditContext.ActorId);
        rejection.DocumentId.ShouldBeNull();
        rejection.BookingId.ShouldBeNull();
        rejection.DocumentRevision.ShouldBeNull();
    }

    [Theory]
    [InlineData(DocumentAuditOperation.UpdateField, DocumentStatus.InReview, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.Void, DocumentStatus.Voided, DocumentAuditReasonCode.ManualVoid)]
    public async Task Successful_staff_mutation_commits_the_status_and_audit(
        DocumentAuditOperation operation,
        DocumentStatus expectedStatus,
        DocumentAuditReasonCode expectedReason)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = operation == DocumentAuditOperation.Void
            ? DocumentDraftInfrastructureTestData.CreateFinalizedDraft(now)
            : DocumentDraftInfrastructureTestData.CreateDraft(now);
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(document);

        // Act
        var result = await scenario.MutateDocument(operation, document.Id, auditContext, DocumentVoidReason.AdministrativeVoid, ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var persisted = await scenario.GetDocumentById(document.Id, ct);
        persisted.ShouldNotBeNull().Status.ShouldBe(expectedStatus);
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        var audit = audits.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(operation);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        audit.ReasonCode.ShouldBe(expectedReason);
        audit.DocumentId.ShouldBe(document.Id);
    }

    [Fact]
    public async Task Approval_commits_the_currently_reviewed_document_and_audit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        var document = await scenario.SeedReviewedDraft(TimeProvider.System.GetUtcNow().UtcDateTime, ct);

        // Act
        var result = await scenario.MutateDocument(
            DocumentAuditOperation.Approve, document.Id, auditContext, DocumentVoidReason.AdministrativeVoid, ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var persisted = await scenario.GetDocumentById(document.Id, ct);
        persisted.ShouldNotBeNull().Status.ShouldBe(DocumentStatus.Approved);
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        var audit = audits.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.Approve);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.ManualOperation);
    }
}
