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
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, auditStore);

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
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, auditStore);

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
    public async Task Handle_records_metadata_only_audit_for_finalized_document()
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
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, auditStore);
        var auditContext = new DocumentAuditContext("9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4", "9a3ca841b4354928861c660a6e4e1b99");

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id, auditContext), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.DocumentId.ShouldBe(document.Id);
        audit.BookingId.ShouldBe(document.BookingId);
        audit.DocumentRevision.ShouldBe(document.Revision);
        audit.Operation.ShouldBe(DocumentAuditOperation.Finalize);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.ManualFinalize);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_does_not_save_finalization_when_audit_metadata_is_invalid()
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
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, auditStore);
        var invalidAuditContext = new DocumentAuditContext(
            new string('a', DocumentAuditLimits.MaxActorIdLength + 1),
            "9a3ca841b4354928861c660a6e4e1b99");

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id, invalidAuditContext), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
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
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, auditStore);

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
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, auditStore);

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_saving_when_document_is_missing()
    {
        // Arrange
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, auditStore);

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(Guid.CreateVersion7(), DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
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
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, auditStore);

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(replacement.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        replacement.Status.ShouldBe(DocumentStatus.Finalized);
        previous.Status.ShouldBe(DocumentStatus.Superseded);
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
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System, auditStore);

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(replacement.Id, DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        replacement.Status.ShouldBe(DocumentStatus.Finalized);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
