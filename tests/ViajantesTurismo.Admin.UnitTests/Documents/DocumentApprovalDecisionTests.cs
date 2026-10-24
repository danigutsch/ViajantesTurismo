using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentApprovalDecisionTests
{
    [Fact]
    public void A_generated_draft_can_begin_review_and_is_not_finalized()
    {
        // Arrange
        var now = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);

        // Act
        var canBeginReview = draft.CanBeginReview;
        var isFinalized = draft.IsFinalized;

        // Assert
        canBeginReview.ShouldBeTrue();
        isFinalized.ShouldBeFalse();
    }

    [Fact]
    public void A_draft_under_review_cannot_begin_review_again()
    {
        // Arrange
        var now = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        draft.BeginReview(now).IsSuccess.ShouldBeTrue();

        // Act
        var canBeginReview = draft.CanBeginReview;

        // Assert
        canBeginReview.ShouldBeFalse();
    }

    [Fact]
    public void A_draft_with_requested_changes_can_begin_review_again()
    {
        // Arrange
        var now = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        draft.BeginReview(now).IsSuccess.ShouldBeTrue();
        draft.RequestChanges(now).IsSuccess.ShouldBeTrue();

        // Act
        var canBeginReview = draft.CanBeginReview;

        // Assert
        canBeginReview.ShouldBeTrue();
    }

    [Fact]
    public void A_finalized_draft_is_finalized_and_cannot_begin_review()
    {
        // Arrange
        var now = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        draft.BeginReview(now).IsSuccess.ShouldBeTrue();
        draft.Approve(now).IsSuccess.ShouldBeTrue();
        draft.Finalize("artifact"u8.ToArray(), now).IsSuccess.ShouldBeTrue();

        // Act
        var isFinalized = draft.IsFinalized;
        var canBeginReview = draft.CanBeginReview;

        // Assert
        isFinalized.ShouldBeTrue();
        canBeginReview.ShouldBeFalse();
    }

    [Fact]
    public void Only_voided_or_superseded_revisions_are_retired()
    {
        // Arrange
        var now = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create();
        var auditContext = DocumentAuditTestData.CreateContext();
        var older = lineage.Revisions.ShouldHaveSingleItem();
        lineage.BeginReview(older.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(older.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Finalize(older.Id, "older"u8.ToArray(), now, auditContext).IsSuccess.ShouldBeTrue();
        var finalizedIsRetired = older.IsRetired;
        var newer = lineage.CreateRevision(older.Id, DocumentLineageTestData.CreateContent("2"), now, auditContext).Value;
        lineage.BeginReview(newer.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(newer.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Finalize(newer.Id, "newer"u8.ToArray(), now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Void(newer.Id, "administrative-void", now, auditContext).IsSuccess.ShouldBeTrue();

        // Act
        var supersededIsRetired = older.IsRetired;
        var voidedIsRetired = newer.IsRetired;

        // Assert
        finalizedIsRetired.ShouldBeFalse();
        supersededIsRetired.ShouldBeTrue();
        voidedIsRetired.ShouldBeTrue();
    }

    [Fact]
    public void Withdrawing_approval_of_an_approved_revision_requests_changes()
    {
        // Arrange
        var now = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create();
        var auditContext = DocumentAuditTestData.CreateContext();
        var revision = lineage.Revisions.ShouldHaveSingleItem();
        lineage.BeginReview(revision.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(revision.Id, now, auditContext).IsSuccess.ShouldBeTrue();

        // Act
        var result = lineage.WithdrawApproval(revision.Id, now, auditContext);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        revision.Status.ShouldBe(DocumentStatus.ChangesRequested);
    }

    [Fact]
    public void Withdrawing_approval_of_a_finalized_revision_voids_it()
    {
        // Arrange
        var now = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create();
        var auditContext = DocumentAuditTestData.CreateContext();
        var revision = lineage.Revisions.ShouldHaveSingleItem();
        lineage.BeginReview(revision.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(revision.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Finalize(revision.Id, "artifact"u8.ToArray(), now, auditContext).IsSuccess.ShouldBeTrue();

        // Act
        var result = lineage.WithdrawApproval(revision.Id, now, auditContext);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        revision.Status.ShouldBe(DocumentStatus.Voided);
        revision.VoidReason.ShouldBe("approval-withdrawn");
    }

    [Fact]
    public void Withdrawing_approval_of_an_unknown_revision_returns_not_found()
    {
        // Arrange
        var now = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create();

        // Act
        var result = lineage.WithdrawApproval(Guid.CreateVersion7(), now, DocumentAuditTestData.CreateContext());

        // Assert
        result.Status.ShouldBe(PandaBear.Results.ResultStatus.NotFound);
    }
}
