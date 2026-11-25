using Microsoft.EntityFrameworkCore;
using SharedKernel.Idempotency;
using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[Trait(TestTraitNames.CapabilityName, ViajantesTurismo.Admin.Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class DocumentGenerationTransactionPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Theory]
    [InlineData(DocumentAuditOperation.Generate, DocumentAuditReasonCode.BookingNotFound)]
    [InlineData(DocumentAuditOperation.Regenerate, DocumentAuditReasonCode.DocumentNotFound)]
    public async Task Missing_generation_source_keeps_the_key_unclaimed_and_persists_one_rejection(
        DocumentAuditOperation operation,
        DocumentAuditReasonCode reason)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var id = Guid.CreateVersion7();
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        var scope = operation == DocumentAuditOperation.Generate
            ? new GenerateContractDraftCommand(id, auditContext, key).Scope
            : new RegenerateDocumentDraftCommand(id, auditContext, key).Scope;
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        var existing = DocumentDraftInfrastructureTestData.CreateDraft(TimeProvider.System.GetUtcNow().UtcDateTime);
        await scenario.Seed(existing);

        // Act
        var result = await scenario.GenerateDocument(operation, id, auditContext, key, ct);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        var state = await scenario.GetIdempotencyState(scope, key, ct);
        state.ShouldBeNull();
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        var rejection = audits.ShouldHaveSingleItem();
        rejection.Operation.ShouldBe(operation);
        rejection.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        rejection.ReasonCode.ShouldBe(reason);
        rejection.DocumentId.ShouldBeNull();
        rejection.DocumentRevision.ShouldBeNull();
        Guid? expectedBookingId = operation == DocumentAuditOperation.Generate ? id : null;
        rejection.BookingId.ShouldBe(expectedBookingId);
    }

    [Theory]
    [InlineData(DocumentAuditOperation.Generate)]
    [InlineData(DocumentAuditOperation.Regenerate)]
    public async Task Audit_insert_failure_rolls_back_the_document_and_retains_the_processing_claim(
        DocumentAuditOperation operation)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        var nextAttemptContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var id = operation == DocumentAuditOperation.Generate
            ? await scenario.SeedConfirmedBooking(now, ct)
            : (await scenario.SeedReviewedDraft(now, ct)).Id;
        await scenario.SeedDocumentBranding(ct);
        var documentsBeforeFailure = await scenario.GetDocuments(ct);
        var scope = operation == DocumentAuditOperation.Generate
            ? new GenerateContractDraftCommand(id, auditContext, key).Scope
            : new RegenerateDocumentDraftCommand(id, auditContext, key).Scope;
        await scenario.InstallAuditInsertFailure(ct);

        // Act
        Func<Task> fail = async () => _ = await scenario.GenerateDocument(operation, id, auditContext, key, ct);
        _ = await fail.ShouldThrow<DbUpdateException>();
        var stateAfterFailure = await scenario.GetIdempotencyState(scope, key, ct);
        var documentsAfterFailure = await scenario.GetDocuments(ct);
        var auditsAfterFailure = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        await scenario.RestoreAuditInserts(ct);
        var nextAttempt = await scenario.GenerateDocument(operation, id, nextAttemptContext, key, ct);

        // Assert
        stateAfterFailure.ShouldBe("Started");
        documentsAfterFailure.Select(document => document.Id).ShouldBe(documentsBeforeFailure.Select(document => document.Id));
        auditsAfterFailure.ShouldBeEmpty();
        nextAttempt.Status.ShouldBe(ResultStatus.Conflict);
        var audits = await scenario.GetDocumentAuditsForCorrelation(nextAttemptContext.CorrelationId, ct);
        var rejection = audits.ShouldHaveSingleItem();
        rejection.Operation.ShouldBe(operation);
        rejection.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        rejection.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
    }
}
