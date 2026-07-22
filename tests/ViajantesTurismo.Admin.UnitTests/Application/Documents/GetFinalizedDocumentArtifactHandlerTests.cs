using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class GetFinalizedDocumentArtifactHandlerTests
{
    [Fact]
    public async Task Handle_returns_the_sealed_artifact_for_a_finalized_document()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        document.Approve(now).IsSuccess.ShouldBeTrue();
        document.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var handler = new GetFinalizedDocumentArtifactHandler(store);

        // Act
        var result = await handler.Handle(new GetFinalizedDocumentArtifactQuery(document.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.DocumentId.ShouldBe(document.Id);
        result.Value.BookingId.ShouldBe(document.BookingId);
        result.Value.Revision.ShouldBe(document.Revision);
        result.Value.Content.Span.SequenceEqual("artifact"u8).ShouldBeTrue();
        result.Value.FileName.ShouldBe(document.FinalizedArtifactName);
    }

    [Fact]
    public async Task Handle_rejects_an_unfinalized_document_without_an_artifact()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var handler = new GetFinalizedDocumentArtifactHandler(store);

        // Act
        var result = await handler.Handle(new GetFinalizedDocumentArtifactQuery(document.Id), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }
}
