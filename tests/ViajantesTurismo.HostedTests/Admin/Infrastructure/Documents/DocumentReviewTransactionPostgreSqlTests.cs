using Microsoft.Extensions.Logging;
using SharedKernel.Results;
using SharedKernel.Testing;
using SharedKernel.Testing.Logging;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Documents;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[Trait(TestTraitNames.CapabilityName, ViajantesTurismo.Admin.Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class DocumentReviewTransactionPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Rejected_review_keeps_the_finalized_document_and_persists_one_audit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftInfrastructureTestData.CreateFinalizedDraft(now);
        var auditContext = DocumentAuditContext.Create("review-actor", Guid.CreateVersion7().ToString("N")).Value;
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(document);

        // Act
        var result = await scenario.BeginReview(new BeginDocumentReviewCommand(document.Id, auditContext), ct);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var persisted = await scenario.GetDocumentById(document.Id, ct);
        persisted.ShouldNotBeNull().Status.ShouldBe(DocumentStatus.Finalized);
        var audits = await scenario.GetDocumentAuditsForDocument(document.Id, ct);
        var rejection = audits.ShouldHaveSingleItem(entry => entry.CorrelationId == auditContext.CorrelationId);
        rejection.Operation.ShouldBe(DocumentAuditOperation.BeginReview);
        rejection.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        rejection.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        rejection.ActorId.ShouldBe(auditContext.ActorId);
        rejection.BookingId.ShouldBe(document.BookingId);
        rejection.DocumentRevision.ShouldBe(document.Revision);
    }

    [Fact]
    public async Task Missing_document_review_persists_one_redacted_rejection()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        var command = new BeginDocumentReviewCommand(Guid.CreateVersion7(), auditContext);
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        var existing = DocumentDraftInfrastructureTestData.CreateDraft(TimeProvider.System.GetUtcNow().UtcDateTime);
        await scenario.Seed(existing);

        // Act
        var result = await scenario.BeginReview(command, ct);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var expectedDetail = DocumentErrors.DocumentNotFound().ErrorDetails.ShouldNotBeNull().Detail;
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(expectedDetail);
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        var rejection = audits.ShouldHaveSingleItem();
        rejection.Operation.ShouldBe(DocumentAuditOperation.BeginReview);
        rejection.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        rejection.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        rejection.ActorId.ShouldBe(auditContext.ActorId);
        rejection.DocumentId.ShouldBeNull();
        rejection.BookingId.ShouldBeNull();
        rejection.DocumentRevision.ShouldBeNull();
    }

    [Fact]
    public async Task Audit_write_failure_logs_the_hook_and_preserves_the_review_failure()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var document = DocumentDraftInfrastructureTestData.CreateFinalizedDraft(TimeProvider.System.GetUtcNow().UtcDateTime);
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        var logger = new RecordingLogger<AdminTransactionBehavior<BeginDocumentReviewCommand, Result>>();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(document);
        await scenario.InstallAuditInsertFailure(ct);

        // Act
        var result = await scenario.BeginReview(new BeginDocumentReviewCommand(document.Id, auditContext), logger, ct);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var expected = DocumentErrors.InvalidStatusTransition(DocumentStatus.Finalized, DocumentStatus.InReview);
        var expectedDetail = expected.ErrorDetails.ShouldNotBeNull().Detail;
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(expectedDetail);
        var logs = logger.Records;
        var hookFailure = logs.ShouldHaveSingleItem(record => record.Level == LogLevel.Error);
        hookFailure.EventId.Id.ShouldBe(2001);
        hookFailure.EventId.Name.ShouldBe("OutcomeHookFailed");
        hookFailure.Message.ShouldContain(nameof(DocumentRejectionRollbackAudit<BeginDocumentReviewCommand, Result>), StringComparison.Ordinal);
        hookFailure.Message.ShouldNotContain(auditContext.ActorId, StringComparison.Ordinal);
        hookFailure.Message.ShouldNotContain(auditContext.CorrelationId, StringComparison.Ordinal);
        var persisted = await scenario.GetDocumentById(document.Id, ct);
        persisted.ShouldNotBeNull().Status.ShouldBe(DocumentStatus.Finalized);
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        audits.ShouldBeEmpty();
    }

    [Fact]
    public async Task Successful_review_commits_the_status_and_its_success_audit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var document = DocumentDraftInfrastructureTestData.CreateDraft(TimeProvider.System.GetUtcNow().UtcDateTime);
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        await using var scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, ct);
        await scenario.Seed(document);

        // Act
        var result = await scenario.BeginReview(new BeginDocumentReviewCommand(document.Id, auditContext), ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var persisted = await scenario.GetDocumentById(document.Id, ct);
        persisted.ShouldNotBeNull().Status.ShouldBe(DocumentStatus.InReview);
        var audits = await scenario.GetDocumentAuditsForCorrelation(auditContext.CorrelationId, ct);
        var audit = audits.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.BeginReview);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.ManualOperation);
        audit.DocumentId.ShouldBe(document.Id);
    }
}
