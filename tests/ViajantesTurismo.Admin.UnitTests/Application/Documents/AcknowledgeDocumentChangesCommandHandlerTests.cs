using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class AcknowledgeDocumentChangesCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Acknowledge_saves_the_acknowledgement_and_unblocks_approval()
    {
        // Arrange
        var (store, revision) = DocumentManualReviewTestData.CreateStoreWithChangedRevision();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var writer = DocumentAuditTestData.CreateWriter(auditStore, unitOfWork);
        var acknowledge = new AcknowledgeDocumentChangesCommandHandler(store, unitOfWork, TimeProvider.System, writer);
        var approve = new ApproveDocumentCommandHandler(
            store, unitOfWork, TimeProvider.System, writer, new FakeDocumentSourceVersionReader("SOURCE-VERSION-2"));

        // Act
        var acknowledged = await acknowledge.Handle(
            new AcknowledgeDocumentChangesCommand(revision.Id, DocumentAuditTestData.CreateContext()),
            CancellationToken.None);
        var approved = await approve.Handle(
            new ApproveDocumentCommand(revision.Id, DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        acknowledged.IsSuccess.ShouldBeTrue();
        approved.IsSuccess.ShouldBeTrue();
        revision.Status.ShouldBe(DocumentStatus.Approved);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(2);
        auditStore.Records.ShouldBeEmpty();
        var operations = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .OfType<DocumentLifecycleAuditDomainEvent>()
            .Select(domainEvent => domainEvent.Operation)
            .ToList();
        operations.ShouldBe([DocumentAuditOperation.AcknowledgeChanges, DocumentAuditOperation.Approve]);
    }

    [Fact]
    public async Task Approve_returns_a_conflict_and_audits_a_rejection_while_changes_are_unacknowledged()
    {
        // Arrange
        var (store, revision) = DocumentManualReviewTestData.CreateStoreWithChangedRevision();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var approve = new ApproveDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            new FakeDocumentSourceVersionReader("SOURCE-VERSION-2"));

        // Act
        var result = await approve.Handle(
            new ApproveDocumentCommand(revision.Id, DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(
            DocumentErrors.ManualReviewRequired().ErrorDetails.ShouldNotBeNull().Detail);
        revision.Status.ShouldBe(DocumentStatus.InReview);
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.Approve);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
    }

    [Fact]
    public async Task Acknowledge_audits_a_rejection_when_no_changes_await_acknowledgement()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(Now);
        document.BeginReview(Now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AcknowledgeDocumentChangesCommandHandler(
            store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new AcknowledgeDocumentChangesCommand(document.Id, DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.AcknowledgeChanges);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
    }

    [Fact]
    public async Task Acknowledge_audits_a_rejection_for_a_missing_document()
    {
        // Arrange
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AcknowledgeDocumentChangesCommandHandler(
            new FakeDocumentStore(),
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new AcknowledgeDocumentChangesCommand(Guid.CreateVersion7(), DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        auditStore.Records.ShouldHaveSingleItem().ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
    }

    [Fact]
    public async Task Acknowledge_requires_an_audit_context_before_any_mutation()
    {
        // Arrange
        var (store, revision) = DocumentManualReviewTestData.CreateStoreWithChangedRevision();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AcknowledgeDocumentChangesCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(new FakeDocumentAuditStore(), unitOfWork));

        // Act
        var result = await handler.Handle(new AcknowledgeDocumentChangesCommand(revision.Id, null!), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        revision.RequiresManualReview.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Acknowledge_rejects_a_payment_instruction_document()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(Now, type: DocumentType.PaymentInstructions);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new AcknowledgeDocumentChangesCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(new FakeDocumentAuditStore(), unitOfWork));

        // Act
        var result = await handler.Handle(
            new AcknowledgeDocumentChangesCommand(document.Id, DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
