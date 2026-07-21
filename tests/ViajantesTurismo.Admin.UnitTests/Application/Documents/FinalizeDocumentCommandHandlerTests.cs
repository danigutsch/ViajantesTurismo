using System.Text;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class FinalizeDocumentCommandHandlerTests
{
    [Fact]
    public async Task Handle_finalizes_approved_document_and_saves()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Finalized);
        var finalizedArtifact = document.GetFinalizedArtifactContent().ShouldNotBeNull();
        var html = Encoding.UTF8.GetString(finalizedArtifact.Span);
        html.ShouldBeWellFormedHtmlDocument();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_omits_remote_branding_logo_from_finalized_artifact()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now, new Uri("https://branding.example.test/logo.svg", UriKind.Absolute));
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var finalizedArtifact = document.GetFinalizedArtifactContent().ShouldNotBeNull();
        var html = Encoding.UTF8.GetString(finalizedArtifact.Span);
        html.ShouldNotContain("<img", StringComparison.Ordinal);
        html.ShouldNotContain("branding.example.test", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_queues_metadata_only_audit_event_for_finalized_document()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));
        var auditContext = DocumentAuditTestData.CreateContext();

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id, auditContext), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        auditStore.Records.ShouldBeEmpty();
        var auditEvent = store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents()
            .ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.ActorId.ShouldBe(auditContext.ActorId);
        auditEvent.DocumentId.ShouldBe(document.Id);
        auditEvent.BookingId.ShouldBe(document.BookingId);
        auditEvent.DocumentRevision.ShouldBe(document.Revision);
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.Finalize);
        auditEvent.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_does_not_save_finalization_without_audit_metadata()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));
        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Approved);
        document.GetFinalizedArtifactContent().ShouldBeNull();
        store.LastLoadedLineage.ShouldBeNull();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_preserves_template_field_order_in_finalized_artifact()
    {
        // Arrange
        var now = DateTime.UtcNow;
        DocumentField[] fields =
        [
            DocumentField.Create("z-template-first", "Template first", "First value", DocumentPrivacyClassification.Public, false).Value,
            DocumentField.Create("a-template-second", "Template second", "Second value", DocumentPrivacyClassification.Operational, false).Value,
        ];
        var documentResult = DocumentDraft.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            "tour-service-contract",
            "1",
            "SOURCE-VERSION",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            null,
            now);
        documentResult.IsSuccess.ShouldBeTrue();
        var document = documentResult.Value;
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var finalizedArtifact = document.GetFinalizedArtifactContent().ShouldNotBeNull();
        var html = Encoding.UTF8.GetString(finalizedArtifact.Span);
        var firstIndex = html.IndexOf("Template first", StringComparison.Ordinal);
        var secondIndex = html.IndexOf("Template second", StringComparison.Ordinal);
        firstIndex.ShouldBeGreaterThanOrEqualTo(0);
        secondIndex.ShouldBeGreaterThan(firstIndex);
    }

    [Fact]
    public async Task Handle_rejects_unapproved_document_without_saving()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id, auditContext), CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.DraftGenerated);
        store.LastLoadedLineage.ShouldNotBeNull().GetDomainEvents().ShouldBeEmpty();
        audit.Operation.ShouldBe(DocumentAuditOperation.Finalize);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBe(document.Id);
        audit.BookingId.ShouldBe(document.BookingId);
        audit.DocumentRevision.ShouldBe(document.Revision);
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_saving_when_document_is_missing()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(documentId, auditContext), CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        audit.Operation.ShouldBe(DocumentAuditOperation.Finalize);
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
    public async Task Handle_supersedes_previous_finalized_revision_after_replacement_finalizes()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var previous = DocumentDraftTestData.Create(now);
        previous.BeginReview(now).IsSuccess.ShouldBeTrue();
        previous.Approve(now).IsSuccess.ShouldBeTrue();
        previous.Finalize("previous"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        DocumentField[] replacementFields =
        [
            DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
            DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
        ];
        var replacementResult = previous.CreateRevision(
            "tour-service-contract",
            "2",
            "SOURCE-VERSION-2",
            replacementFields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            new Uri("/logo.svg", UriKind.Relative),
            now);
        replacementResult.IsSuccess.ShouldBeTrue();
        var replacement = replacementResult.Value;
        replacement.BeginReview(now).IsSuccess.ShouldBeTrue();
        replacement.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(previous.Id, previous);
        store.Documents.Add(replacement.Id, replacement);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(replacement.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        replacement.Status.ShouldBe(DocumentStatus.Finalized);
        previous.Status.ShouldBe(DocumentStatus.Superseded);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_supersedes_active_finalized_ancestor_when_predecessor_is_unfinalized()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var first = DocumentDraftTestData.Create(now);
        first.BeginReview(now).IsSuccess.ShouldBeTrue();
        first.Approve(now).IsSuccess.ShouldBeTrue();
        first.Finalize("first"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        DocumentField[] fields =
        [
            DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
            DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
        ];
        var secondResult = first.CreateRevision(
            "tour-service-contract",
            "2",
            "SOURCE-VERSION-2",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            new Uri("/logo.svg", UriKind.Relative),
            now);
        secondResult.IsSuccess.ShouldBeTrue();
        var second = secondResult.Value;
        var thirdResult = second.CreateRevision(
            "tour-service-contract",
            "3",
            "SOURCE-VERSION-3",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            new Uri("/logo.svg", UriKind.Relative),
            now);
        thirdResult.IsSuccess.ShouldBeTrue();
        var third = thirdResult.Value;
        third.BeginReview(now).IsSuccess.ShouldBeTrue();
        third.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(first.Id, first);
        store.Documents.Add(second.Id, second);
        store.Documents.Add(third.Id, third);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new FinalizeDocumentCommand(third.Id, DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        first.Status.ShouldBe(DocumentStatus.Superseded);
        second.Status.ShouldBe(DocumentStatus.DraftGenerated);
        third.Status.ShouldBe(DocumentStatus.Finalized);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_keeps_replacement_finalized_when_previous_revision_is_missing()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var previous = DocumentDraftTestData.Create(now);
        DocumentField[] replacementFields =
        [
            DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
            DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
        ];
        var replacementResult = previous.CreateRevision(
            "tour-service-contract",
            "2",
            "SOURCE-VERSION-2",
            replacementFields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            new Uri("/logo.svg", UriKind.Relative),
            now);
        replacementResult.IsSuccess.ShouldBeTrue();
        var replacement = replacementResult.Value;
        replacement.BeginReview(now).IsSuccess.ShouldBeTrue();
        replacement.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(replacement.Id, replacement);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(replacement.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        replacement.Status.ShouldBe(DocumentStatus.Finalized);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_rejects_finalizing_an_older_revision_after_a_newer_revision()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var older = DocumentDraftTestData.Create(now);
        DocumentField[] replacementFields =
        [
            DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
            DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
        ];
        var newerResult = older.CreateRevision(
            "tour-service-contract",
            "2",
            "SOURCE-VERSION-2",
            replacementFields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            new Uri("/logo.svg", UriKind.Relative),
            now);
        newerResult.IsSuccess.ShouldBeTrue();
        var newer = newerResult.Value;
        newer.BeginReview(now).IsSuccess.ShouldBeTrue();
        newer.Approve(now).IsSuccess.ShouldBeTrue();
        newer.Finalize("newer"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        older.BeginReview(now).IsSuccess.ShouldBeTrue();
        older.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(older.Id, older);
        store.Documents.Add(newer.Id, newer);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new FinalizeDocumentCommand(older.Id, DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        older.Status.ShouldBe(DocumentStatus.Approved);
        newer.Status.ShouldBe(DocumentStatus.Finalized);
        auditStore.Records.ShouldHaveSingleItem().ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_rejects_finalizing_an_older_revision_after_a_newer_revision_was_voided()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var older = DocumentDraftTestData.Create(now);
        DocumentField[] replacementFields =
        [
            DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
            DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
        ];
        var newerResult = older.CreateRevision(
            "tour-service-contract",
            "2",
            "SOURCE-VERSION-2",
            replacementFields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            new Uri("/logo.svg", UriKind.Relative),
            now);
        newerResult.IsSuccess.ShouldBeTrue();
        var newer = newerResult.Value;
        newer.BeginReview(now).IsSuccess.ShouldBeTrue();
        newer.Approve(now).IsSuccess.ShouldBeTrue();
        newer.Finalize("newer"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        newer.Void("Superseded contract cancelled", now).IsSuccess.ShouldBeTrue();
        older.BeginReview(now).IsSuccess.ShouldBeTrue();
        older.Approve(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(older.Id, older);
        store.Documents.Add(newer.Id, newer);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(
            store,
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new FinalizeDocumentCommand(older.Id, DocumentAuditTestData.CreateContext()),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        older.Status.ShouldBe(DocumentStatus.Approved);
        newer.Status.ShouldBe(DocumentStatus.Voided);
        auditStore.Records.ShouldHaveSingleItem().ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

}
