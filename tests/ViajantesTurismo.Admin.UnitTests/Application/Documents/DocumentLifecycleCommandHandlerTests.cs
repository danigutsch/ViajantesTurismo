using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentLifecycleCommandHandlerTests
{
    [Fact]
    public async Task BeginReview_saves_eligible_draft()
    {
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new BeginDocumentReviewCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new BeginDocumentReviewCommand(document.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.InReview);
        var auditEvent = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.BeginReview);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task BeginReview_rejects_missing_audit_context_before_mutation()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new BeginDocumentReviewCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new BeginDocumentReviewCommand(document.Id), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.DraftGenerated);
        store.LastLoadedLineage.ShouldBeNull();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task BeginReview_does_not_save_missing_draft()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new BeginDocumentReviewCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new BeginDocumentReviewCommand(documentId, auditContext), CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.Operation.ShouldBe(DocumentAuditOperation.BeginReview);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBe(documentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Void_saves_finalized_draft()
    {
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new VoidDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new VoidDocumentCommand(document.Id, "customer-cancellation", DocumentAuditTestData.CreateContext()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Voided);
        var auditEvent = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.Void);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Void_audits_invalid_reason_as_validation_rejected()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new VoidDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new VoidDocumentCommand(document.Id, " ", DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        auditStore.Records.ShouldHaveSingleItem().ReasonCode.ShouldBe(DocumentAuditReasonCode.ValidationRejected);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Approve_saves_document_in_review()
    {
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new ApproveDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new ApproveDocumentCommand(document.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Approved);
        var auditEvent = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.Approve);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Approve_rejects_missing_audit_context_before_mutation()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ApproveDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new ApproveDocumentCommand(document.Id), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.InReview);
        store.LastLoadedLineage.ShouldBeNull();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Approve_rejects_missing_document_and_records_audit()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new ApproveDocumentCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new ApproveDocumentCommand(documentId, auditContext), CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.Operation.ShouldBe(DocumentAuditOperation.Approve);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBe(documentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RequestChanges_saves_document_in_review()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new RequestDocumentChangesCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new RequestDocumentChangesCommand(document.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.ChangesRequested);
        var auditEvent = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.RequestChanges);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RequestChanges_rejects_missing_audit_context_before_mutation()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RequestDocumentChangesCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new RequestDocumentChangesCommand(document.Id), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.InReview);
        store.LastLoadedLineage.ShouldBeNull();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RequestChanges_does_not_save_missing_draft()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new RequestDocumentChangesCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new RequestDocumentChangesCommand(documentId, auditContext), CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.Operation.ShouldBe(DocumentAuditOperation.RequestChanges);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBe(documentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Void_does_not_save_missing_draft()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new VoidDocumentCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new VoidDocumentCommand(documentId, "customer-cancellation", auditContext), CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.Operation.ShouldBe(DocumentAuditOperation.Void);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBe(documentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Void_rejects_missing_audit_context_before_mutation()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new VoidDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new VoidDocumentCommand(document.Id, "customer-cancellation"),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Finalized);
        store.LastLoadedLineage.ShouldBeNull();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Purge_removes_only_eligible_drafts_and_reports_removed_count()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero);
        var expired = DocumentDraftTestData.Create(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        var current = DocumentDraftTestData.Create(now.UtcDateTime);
        var store = new FakeDocumentStore();
        store.Documents.Add(expired.Id, expired);
        store.Documents.Add(current.Id, current);
        // Act
        var removedCount = await new PurgeExpiredDraftsCommandHandler(store, new FakeTimeProvider(now))
            .Handle(new PurgeExpiredDraftsCommand(), CancellationToken.None);

        // Assert
        removedCount.ShouldBe(1);
        store.Documents.ContainsKey(expired.Id).ShouldBeFalse();
        store.Documents.ContainsKey(current.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateField_saves_editable_field_override()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new UpdateDocumentFieldCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new UpdateDocumentFieldCommand(document.Id, "greeting", "Welcome", DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        document.Fields.Single(field => field.FieldId == "greeting").RenderedValue.ShouldBe("Welcome");
        var auditEvent = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.UpdateField);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateField_records_state_conflict_for_an_immutable_document()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateDocumentFieldCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new UpdateDocumentFieldCommand(document.Id, "greeting", "Welcome", DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Conflict);
        document.Status.ShouldBe(DocumentStatus.Finalized);
        auditStore.Records.ShouldHaveSingleItem().ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateField_rejects_and_audits_a_null_value()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateDocumentFieldCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new UpdateDocumentFieldCommand(document.Id, "greeting", null!, DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        auditStore.Records.ShouldHaveSingleItem().ReasonCode.ShouldBe(DocumentAuditReasonCode.ValidationRejected);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateField_rejects_missing_audit_context_before_mutation()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var originalValue = document.Fields.Single(field => field.FieldId == "greeting").RenderedValue;
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateDocumentFieldCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new UpdateDocumentFieldCommand(document.Id, "greeting", "Changed", null),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Fields.Single(field => field.FieldId == "greeting").RenderedValue.ShouldBe(originalValue);
        store.LastLoadedLineage.ShouldBeNull();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateField_rejects_missing_document_and_records_audit()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new UpdateDocumentFieldCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork))
            .Handle(new UpdateDocumentFieldCommand(documentId, "greeting", "Welcome", auditContext), CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.Operation.ShouldBe(DocumentAuditOperation.UpdateField);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBe(documentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
