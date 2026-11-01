using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentLineageLifecycleTests
{
    [Fact]
    public void Finalize_rejects_an_older_revision_after_a_newer_revision_was_finalized_and_voided()
    {
        // Arrange
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create();
        var auditContext = DocumentAuditTestData.CreateContext();
        var older = lineage.Revisions.ShouldHaveSingleItem();
        var newerResult = lineage.CreateRevision(older.Id, DocumentLineageTestData.CreateContent("2"), now, auditContext);
        newerResult.IsSuccess.ShouldBeTrue();
        var newer = newerResult.Value;
        lineage.BeginReview(newer.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(newer.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Finalize(newer.Id, "newer"u8.ToArray(), now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Void(newer.Id, DocumentVoidReason.AdministrativeVoid, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.BeginReview(older.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(older.Id, now, auditContext).IsSuccess.ShouldBeTrue();

        // Act
        var result = lineage.Finalize(older.Id, "older"u8.ToArray(), now, auditContext);

        // Assert
        result.IsFailure.ShouldBeTrue();
        lineage.HighestFinalizedRevision.ShouldBe(2);
        older.Status.ShouldBe(DocumentStatus.Approved);
        newer.Status.ShouldBe(DocumentStatus.Voided);
    }

    [Fact]
    public void Finalize_newer_revision_supersedes_the_active_older_finalization()
    {
        // Arrange
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create();
        var auditContext = DocumentAuditTestData.CreateContext();
        var older = lineage.Revisions.ShouldHaveSingleItem();
        lineage.BeginReview(older.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(older.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Finalize(older.Id, "older"u8.ToArray(), now, auditContext).IsSuccess.ShouldBeTrue();
        var newerResult = lineage.CreateRevision(older.Id, DocumentLineageTestData.CreateContent("2"), now, auditContext);
        newerResult.IsSuccess.ShouldBeTrue();
        var newer = newerResult.Value;
        lineage.BeginReview(newer.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(newer.Id, now, auditContext).IsSuccess.ShouldBeTrue();

        // Act
        var result = lineage.Finalize(newer.Id, "newer"u8.ToArray(), now, auditContext);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        lineage.HighestFinalizedRevision.ShouldBe(2);
        older.Status.ShouldBe(DocumentStatus.Superseded);
        newer.Status.ShouldBe(DocumentStatus.Finalized);
    }

    [Fact]
    public void Ensure_can_approve_and_finalize_follow_the_revision_status_without_mutating()
    {
        // Arrange
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentLineageTestData.Create();
        var auditContext = DocumentAuditTestData.CreateContext();
        var revision = lineage.Revisions.ShouldHaveSingleItem();
        lineage.EnsureCanApprove(revision.Id).Status.ShouldBe(PandaBear.Results.ResultStatus.Conflict);
        lineage.BeginReview(revision.Id, now, auditContext).IsSuccess.ShouldBeTrue();

        // Act and Assert
        lineage.EnsureCanApprove(revision.Id).IsSuccess.ShouldBeTrue();
        lineage.EnsureCanFinalize(revision.Id).Status.ShouldBe(PandaBear.Results.ResultStatus.Conflict);
        lineage.Approve(revision.Id, now, auditContext).IsSuccess.ShouldBeTrue();
        lineage.EnsureCanApprove(revision.Id).Status.ShouldBe(PandaBear.Results.ResultStatus.Conflict);
        lineage.EnsureCanFinalize(revision.Id).IsSuccess.ShouldBeTrue();
        revision.Status.ShouldBe(DocumentStatus.Approved);
    }

    [Fact]
    public void Ensure_can_approve_and_finalize_report_an_unknown_revision_as_not_found()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create();

        // Act and Assert
        lineage.EnsureCanApprove(Guid.CreateVersion7()).Status.ShouldBe(PandaBear.Results.ResultStatus.NotFound);
        lineage.EnsureCanFinalize(Guid.CreateVersion7()).Status.ShouldBe(PandaBear.Results.ResultStatus.NotFound);
    }
}
