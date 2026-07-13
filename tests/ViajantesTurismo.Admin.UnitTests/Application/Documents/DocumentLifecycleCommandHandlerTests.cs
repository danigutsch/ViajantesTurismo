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
    public async Task Approve_saves_document_in_review()
    {
        var now = DateTime.UtcNow;
        var document = DocumentDraftTestData.Create(now);
        document.BeginReview(now).IsSuccess.ShouldBeTrue();
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new ApproveDocumentCommandHandler(store, unitOfWork, TimeProvider.System)
            .Handle(new ApproveDocumentCommand(document.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Approved);
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
        object removedCount = await new PurgeExpiredDraftsCommandHandler(store, new FakeTimeProvider(now))
            .Handle(new PurgeExpiredDraftsCommand(), CancellationToken.None);

        // Assert
        var rawRemovedCount = removedCount.ShouldBeOfType<int>();
        rawRemovedCount.ShouldBe(1);
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
        var unitOfWork = new FakeUnitOfWork();

        // Act
        var result = await new UpdateDocumentFieldCommandHandler(store, unitOfWork, TimeProvider.System)
            .Handle(new UpdateDocumentFieldCommand(document.Id, "greeting", "Welcome"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        document.Fields.Single(field => field.FieldId == "greeting").RenderedValue.ShouldBe("Welcome");
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
