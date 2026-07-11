using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, "generated-documents")]
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
}
