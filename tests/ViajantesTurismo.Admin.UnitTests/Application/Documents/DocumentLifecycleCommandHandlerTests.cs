using SharedKernel.Testing;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, "generated-documents")]
public sealed class DocumentLifecycleCommandHandlerTests
{
    [Fact]
    public async Task BeginReview_saves_eligible_draft()
    {
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new BeginDocumentReviewCommandHandler(store, unitOfWork, TimeProvider.System)
            .Handle(new BeginDocumentReviewCommand(document.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.InReview);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task BeginReview_does_not_save_missing_draft()
    {
        var unitOfWork = new FakeUnitOfWork();

        var result = await new BeginDocumentReviewCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System)
            .Handle(new BeginDocumentReviewCommand(Guid.CreateVersion7()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
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
        var unitOfWork = new FakeUnitOfWork();

        var result = await new VoidDocumentCommandHandler(store, unitOfWork, TimeProvider.System)
            .Handle(new VoidDocumentCommand(document.Id, "customer-cancellation"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Voided);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Void_does_not_save_missing_draft()
    {
        var unitOfWork = new FakeUnitOfWork();

        var result = await new VoidDocumentCommandHandler(new FakeDocumentStore(), unitOfWork, TimeProvider.System)
            .Handle(new VoidDocumentCommand(Guid.CreateVersion7(), "customer-cancellation"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
