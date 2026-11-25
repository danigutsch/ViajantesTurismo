using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(TestTraitNames.CapabilityName, ViajantesTurismo.Admin.Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class DocumentChangeDecisionTransactionPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Theory]
    [InlineData(DocumentAuditOperation.RequestChanges)]
    [InlineData(DocumentAuditOperation.AcknowledgeChanges)]
    public async Task Rejected_change_decision_preserves_the_document_and_persists_one_audit(DocumentAuditOperation operation)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var document = DocumentDraftInfrastructureTestData.CreateFinalizedDraft(TimeProvider.System.GetUtcNow().UtcDateTime);
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(document);

        // Act
        var result = await scenario.DecideDocumentChanges(operation, document.Id, auditContext, ct);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        var persisted = await scenario.GetDocumentById(document.Id, ct);
        persisted.ShouldNotBeNull().Status.ShouldBe(DocumentStatus.Finalized);
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        var rejection = audits.ShouldHaveSingleItem();
        rejection.Operation.ShouldBe(operation);
        rejection.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        rejection.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        rejection.DocumentId.ShouldBe(document.Id);
        rejection.BookingId.ShouldBe(document.BookingId);
        rejection.DocumentRevision.ShouldBe(document.Revision);
    }

    [Theory]
    [InlineData(DocumentAuditOperation.RequestChanges)]
    [InlineData(DocumentAuditOperation.AcknowledgeChanges)]
    public async Task Missing_document_for_a_change_decision_persists_one_redacted_rejection(DocumentAuditOperation operation)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        var missingDocumentId = Guid.CreateVersion7();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        var existing = DocumentDraftInfrastructureTestData.CreateDraft(TimeProvider.System.GetUtcNow().UtcDateTime);
        await scenario.Seed(existing);

        // Act
        var result = await scenario.DecideDocumentChanges(operation, missingDocumentId, auditContext, ct);

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
    [InlineData(DocumentAuditOperation.RequestChanges, DocumentStatus.ChangesRequested, true, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.AcknowledgeChanges, DocumentStatus.InReview, false, DocumentAuditReasonCode.ManualAcknowledgement)]
    public async Task Successful_change_decision_commits_the_mutation_and_its_audit(
        DocumentAuditOperation operation,
        DocumentStatus expectedStatus,
        bool requiresManualReview,
        DocumentAuditReasonCode expectedReasonCode)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var setupContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        var auditContext = DocumentAuditContext.Create("review-actor", Guid.CreateVersion7().ToString("N")).Value;
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(now);
        var initial = lineage.Revisions.ShouldHaveSingleItem();
        var replacement = lineage.CreateRevision(
            initial.Id,
            DocumentDraftInfrastructureTestData.CreateContent("2"),
            now,
            setupContext).Value;
        lineage.BeginReview(replacement.Id, now, setupContext).IsSuccess.ShouldBeTrue();
        lineage.ClearDomainEvents();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(lineage);

        // Act
        var result = await scenario.DecideDocumentChanges(operation, replacement.Id, auditContext, ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var persisted = await scenario.GetDocumentById(replacement.Id, ct);
        var updated = persisted.ShouldNotBeNull();
        updated.Status.ShouldBe(expectedStatus);
        updated.RequiresManualReview.ShouldBe(requiresManualReview);
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        var audit = audits.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(operation);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        audit.ReasonCode.ShouldBe(expectedReasonCode);
        audit.DocumentId.ShouldBe(replacement.Id);
    }
}
