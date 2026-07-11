using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, global::ViajantesTurismo.Admin.Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
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
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System);

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Finalized);
        document.GetFinalizedArtifactContent().ShouldNotBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_rejects_unapproved_document_without_saving()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System);

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(document.Id), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_saving_when_document_is_missing()
    {
        // Arrange
        var store = new FakeDocumentStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System);

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(Guid.CreateVersion7()), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
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
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System);

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(replacement.Id), CancellationToken.None);

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
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeDocumentCommandHandler(store, unitOfWork, TimeProvider.System);

        // Act
        var result = await handler.Handle(new FinalizeDocumentCommand(replacement.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        replacement.Status.ShouldBe(DocumentStatus.Finalized);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
