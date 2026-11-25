using ViajantesTurismo.Admin.Application.Documents.Holds;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(TestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentLifecycleCommandHandlerTests
{
    [Fact]
    public async Task BeginReview_saves_eligible_draft()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new BeginDocumentReviewCommandHandler(store, unitOfWork, TimeProvider.System);
        var command = new BeginDocumentReviewCommand(document.Id, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
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
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new BeginDocumentReviewCommandHandler(store, unitOfWork, TimeProvider.System);
        var command = new BeginDocumentReviewCommand(document.Id, null!);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.DraftGenerated);
        store.LastLoadedLineage.ShouldBeNull();
        command.TakeRejectionAudit().ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task BeginReview_defers_a_redacted_audit_when_the_draft_is_missing()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var command = new BeginDocumentReviewCommand(Guid.CreateVersion7(), DocumentAuditTestData.CreateContext());
        var handler = new BeginDocumentReviewCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);
        var audit = command.TakeRejectionAudit().ShouldNotBeNull();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.DocumentLineageId.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Void_saves_finalized_draft()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new VoidDocumentCommandHandler(store, unitOfWork, TimeProvider.System);
        var command = new VoidDocumentCommand(document.Id, DocumentVoidReason.AdministrativeVoid, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Voided);
        var auditEvent = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.Void);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Void_defers_validation_evidence_for_an_invalid_reason()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new VoidDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System);

        var command = new VoidDocumentCommand(document.Id, (DocumentVoidReason)99, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        command.TakeRejectionAudit().ShouldNotBeNull().ReasonCode.ShouldBe(DocumentAuditReasonCode.ValidationRejected);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Approve_saves_document_in_review()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new ApproveDocumentCommandHandler(store, unitOfWork, TimeProvider.System, new FakeDocumentSourceVersionReader())
            .Handle(new ApproveDocumentCommand(document.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Approved);
        var auditEvent = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.Approve);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Approve_rejects_a_stale_source_and_defers_rejection_evidence_without_mutating()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ApproveDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            new FakeDocumentSourceVersionReader("CHANGED-SOURCE-VERSION"));

        var command = new ApproveDocumentCommand(document.Id, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Conflict);
        document.Status.ShouldBe(DocumentStatus.InReview);
        command.TakeRejectionAudit().ShouldNotBeNull().ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
    }

    [Fact]
    public async Task Approve_rejects_a_stale_template_and_defers_rejection_evidence_without_mutating()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now, templateVersion: "0");
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ApproveDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            new FakeDocumentSourceVersionReader());

        var command = new ApproveDocumentCommand(document.Id, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(DocumentErrors.TemplateChangedSinceDraft().ErrorDetails!.Detail);
        document.Status.ShouldBe(DocumentStatus.InReview);
        command.TakeRejectionAudit().ShouldNotBeNull().ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Approve_reports_the_state_conflict_before_a_changed_source_for_a_retired_document(bool voided)
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        if (voided)
        {
            document.Void(DocumentVoidReason.AdministrativeVoid, now).IsSuccess.ShouldBeTrue();
        }

        var status = document.Status;
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ApproveDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            new FakeDocumentSourceVersionReader("CHANGED-SOURCE-VERSION"));

        var command = new ApproveDocumentCommand(document.Id, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(
            DocumentErrors.InvalidStatusTransition(status, DocumentStatus.Approved).ErrorDetails!.Detail);
        document.Status.ShouldBe(status);
        command.TakeRejectionAudit().ShouldNotBeNull().ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
    }

    [Fact]
    public async Task Approve_reports_a_missing_tour_with_the_tour_not_found_audit_reason()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ApproveDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            new FakeDocumentSourceVersionReader { MissingSource = DocumentSourceKind.Tour });

        var command = new ApproveDocumentCommand(document.Id, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        document.Status.ShouldBe(DocumentStatus.InReview);
        command.TakeRejectionAudit().ShouldNotBeNull().ReasonCode.ShouldBe(DocumentAuditReasonCode.TourNotFound);
    }

    [Fact]
    public async Task Approve_reports_a_missing_source_and_defers_rejection_evidence_without_mutating()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ApproveDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            new FakeDocumentSourceVersionReader { MissingSource = DocumentSourceKind.Booking });

        var command = new ApproveDocumentCommand(document.Id, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        document.Status.ShouldBe(DocumentStatus.InReview);
        command.TakeRejectionAudit().ShouldNotBeNull().ReasonCode.ShouldBe(DocumentAuditReasonCode.BookingNotFound);
    }

    [Fact]
    public async Task Approve_rejects_missing_audit_context_before_mutation()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ApproveDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            new FakeDocumentSourceVersionReader());

        var command = new ApproveDocumentCommand(document.Id, null!);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.InReview);
        store.LastLoadedLineage.ShouldBeNull();
        command.TakeRejectionAudit().ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Approve_rejects_missing_document_and_defers_rejection_evidence()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var unitOfWork = new FakeUnitOfWork();

        var command = new ApproveDocumentCommand(documentId, auditContext);

        // Act
        var result = await new ApproveDocumentCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System, new FakeDocumentSourceVersionReader())
            .Handle(command, TestContext.Current.CancellationToken);
        var audit = command.TakeRejectionAudit().ShouldNotBeNull();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.DocumentLineageId.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RequestChanges_saves_document_in_review()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RequestDocumentChangesCommandHandler(store, unitOfWork, TimeProvider.System);
        var command = new RequestDocumentChangesCommand(document.Id, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

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
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RequestDocumentChangesCommandHandler(store, unitOfWork, TimeProvider.System);
        var command = new RequestDocumentChangesCommand(document.Id, null!);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.InReview);
        store.LastLoadedLineage.ShouldBeNull();
        command.TakeRejectionAudit().ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RequestChanges_defers_redacted_rejection_evidence_for_a_missing_document()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RequestDocumentChangesCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System);
        var command = new RequestDocumentChangesCommand(Guid.CreateVersion7(), DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);
        var audit = command.TakeRejectionAudit().ShouldNotBeNull();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.DocumentLineageId.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Void_defers_redacted_rejection_evidence_for_a_missing_draft()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var unitOfWork = new FakeUnitOfWork();

        var command = new VoidDocumentCommand(documentId, DocumentVoidReason.AdministrativeVoid, auditContext);

        // Act
        var result = await new VoidDocumentCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System)
            .Handle(command, TestContext.Current.CancellationToken);
        var audit = command.TakeRejectionAudit().ShouldNotBeNull();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.DocumentLineageId.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Void_rejects_missing_audit_context_before_mutation()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new VoidDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System);

        var command = new VoidDocumentCommand(document.Id, DocumentVoidReason.AdministrativeVoid, null!);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Finalized);
        store.LastLoadedLineage.ShouldBeNull();
        command.TakeRejectionAudit().ShouldBeNull();
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
        var clock = new FakeTimeProvider(now);
        var gate = new DocumentHoldGate(new FakeDispositionHoldGuard(), new InMemoryDispositionHoldStore(), clock);
        var counts = await new PurgeExpiredDraftsCommandHandler(store, DocumentPurgeTestData.CreateRunner(new FakeDocumentPurgeEvidenceStore(), clock), gate, clock)
            .Handle(new PurgeExpiredDraftsCommand(), CancellationToken.None);

        // Assert
        counts.Purged.ShouldBe(1);
        store.Documents.ContainsKey(expired.Id).ShouldBeFalse();
        store.Documents.ContainsKey(current.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateField_saves_editable_field_override()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new UpdateDocumentFieldCommandHandler(store, unitOfWork, TimeProvider.System)
            .Handle(new UpdateDocumentFieldCommand(document.Id, "greeting", "Welcome", DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        document.Fields.ShouldHaveSingleItem(field => field.FieldId == "greeting").RenderedValue.ShouldBe("Welcome");
        var auditEvent = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.UpdateField);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateField_defers_state_conflict_evidence_for_an_immutable_document()
    {
        // Arrange
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateDocumentFieldCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System);

        var command = new UpdateDocumentFieldCommand(document.Id, "greeting", "Welcome", DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Conflict);
        document.Status.ShouldBe(DocumentStatus.Finalized);
        command.TakeRejectionAudit().ShouldNotBeNull().ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateField_rejects_and_defers_validation_evidence_for_a_null_value()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateDocumentFieldCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System);

        var command = new UpdateDocumentFieldCommand(document.Id, "greeting", null, DocumentAuditTestData.CreateContext());

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        command.TakeRejectionAudit().ShouldNotBeNull().ReasonCode.ShouldBe(DocumentAuditReasonCode.ValidationRejected);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateField_rejects_missing_audit_context_before_mutation()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime);
        var originalValue = document.Fields.ShouldHaveSingleItem(field => field.FieldId == "greeting").RenderedValue;
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateDocumentFieldCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System);

        var command = new UpdateDocumentFieldCommand(document.Id, "greeting", "Changed", null!);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Fields.ShouldHaveSingleItem(field => field.FieldId == "greeting").RenderedValue.ShouldBe(originalValue);
        store.LastLoadedLineage.ShouldBeNull();
        command.TakeRejectionAudit().ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateField_rejects_missing_document_and_defers_rejection_evidence()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var unitOfWork = new FakeUnitOfWork();

        var command = new UpdateDocumentFieldCommand(documentId, "greeting", "Welcome", auditContext);

        // Act
        var result = await new UpdateDocumentFieldCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System)
            .Handle(command, TestContext.Current.CancellationToken);
        var audit = command.TakeRejectionAudit().ShouldNotBeNull();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.DocumentLineageId.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
