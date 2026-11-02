using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentManualReviewTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void An_initial_revision_does_not_require_manual_review()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);

        // Act
        var revision = lineage.Revisions.ShouldHaveSingleItem();

        // Assert
        revision.RequiresManualReview.ShouldBeFalse();
        revision.ManualReviewReasons.ShouldBe(DocumentManualReviewReasons.None);
    }

    [Fact]
    public void A_regenerated_revision_with_unchanged_versions_does_not_require_manual_review()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var previous = lineage.Revisions.ShouldHaveSingleItem();

        // Act
        var result = lineage.CreateRevision(
            previous.Id, DocumentLineageTestData.CreateContent(), Now, DocumentAuditTestData.CreateContext());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.RequiresManualReview.ShouldBeFalse();
        result.Value.ManualReviewReasons.ShouldBe(DocumentManualReviewReasons.None);
    }

    [Theory]
    [InlineData("1", "SOURCE-VERSION-NEW", DocumentManualReviewReasons.SourceChanged)]
    [InlineData("2", "SOURCE-VERSION-1", DocumentManualReviewReasons.TemplateChanged)]
    [InlineData("2", "SOURCE-VERSION-NEW", DocumentManualReviewReasons.SourceChanged | DocumentManualReviewReasons.TemplateChanged)]
    public void A_regenerated_revision_requires_manual_review_when_source_or_template_changed(
        string templateVersion,
        string sourceVersion,
        DocumentManualReviewReasons expected)
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var previous = lineage.Revisions.ShouldHaveSingleItem();

        // Act
        var result = lineage.CreateRevision(
            previous.Id,
            DocumentLineageTestData.CreateContent(templateVersion, sourceVersion),
            Now,
            DocumentAuditTestData.CreateContext());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.RequiresManualReview.ShouldBeTrue();
        result.Value.ManualReviewReasons.ShouldBe(expected);
    }

    [Fact]
    public void An_unacknowledged_review_requirement_carries_over_to_the_next_revision()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var context = DocumentAuditTestData.CreateContext();
        var first = lineage.Revisions.ShouldHaveSingleItem();
        var second = lineage.CreateRevision(
            first.Id, DocumentLineageTestData.CreateContent("1", "SOURCE-VERSION-NEW"), Now, context).Value;

        // Act
        var third = lineage.CreateRevision(
            second.Id, DocumentLineageTestData.CreateContent("1", "SOURCE-VERSION-NEW"), Now, context).Value;

        // Assert
        third.RequiresManualReview.ShouldBeTrue();
        third.ManualReviewReasons.ShouldBe(DocumentManualReviewReasons.SourceChanged);
    }

    [Fact]
    public void An_acknowledged_review_requirement_does_not_carry_over_to_an_unchanged_revision()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var context = DocumentAuditTestData.CreateContext();
        var first = lineage.Revisions.ShouldHaveSingleItem();
        var second = lineage.CreateRevision(
            first.Id, DocumentLineageTestData.CreateContent("1", "SOURCE-VERSION-NEW"), Now, context).Value;
        lineage.BeginReview(second.Id, Now, context).IsSuccess.ShouldBeTrue();
        lineage.AcknowledgeChanges(second.Id, Now, context).IsSuccess.ShouldBeTrue();

        // Act
        var third = lineage.CreateRevision(
            second.Id, DocumentLineageTestData.CreateContent("1", "SOURCE-VERSION-NEW"), Now, context).Value;

        // Assert
        third.RequiresManualReview.ShouldBeFalse();
    }

    [Fact]
    public void Approve_is_blocked_with_a_conflict_until_the_changes_are_acknowledged()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var context = DocumentAuditTestData.CreateContext();
        var revision = lineage.CreateRevision(
            lineage.Revisions.ShouldHaveSingleItem().Id,
            DocumentLineageTestData.CreateContent("2"),
            Now,
            context).Value;
        lineage.BeginReview(revision.Id, Now, context).IsSuccess.ShouldBeTrue();
        lineage.ClearDomainEvents();

        // Act
        var result = lineage.Approve(revision.Id, Now, context);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(DocumentErrors.ManualReviewRequired().ErrorDetails.ShouldNotBeNull().Detail);
        revision.Status.ShouldBe(DocumentStatus.InReview);
        lineage.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void Approve_succeeds_after_the_changes_are_acknowledged_and_records_the_acknowledgement()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var context = DocumentAuditTestData.CreateContext();
        var revision = lineage.CreateRevision(
            lineage.Revisions.ShouldHaveSingleItem().Id,
            DocumentLineageTestData.CreateContent("2"),
            Now,
            context).Value;
        lineage.BeginReview(revision.Id, Now, context).IsSuccess.ShouldBeTrue();
        lineage.ClearDomainEvents();

        // Act
        var acknowledge = lineage.AcknowledgeChanges(revision.Id, Now.AddMinutes(1), context);
        var approve = lineage.Approve(revision.Id, Now.AddMinutes(2), context);

        // Assert
        acknowledge.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        revision.RequiresManualReview.ShouldBeFalse();
        revision.ManualReviewReasons.ShouldBe(
            DocumentManualReviewReasons.SourceChanged | DocumentManualReviewReasons.TemplateChanged);
        var operations = lineage.GetDomainEvents()
            .OfType<DocumentLifecycleAuditDomainEvent>()
            .Select(domainEvent => domainEvent.Operation)
            .ToList();
        operations.ShouldBe([DocumentAuditOperation.AcknowledgeChanges, DocumentAuditOperation.Approve]);
    }

    [Fact]
    public void AcknowledgeChanges_requires_the_revision_to_be_under_review()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var context = DocumentAuditTestData.CreateContext();
        var revision = lineage.CreateRevision(
            lineage.Revisions.ShouldHaveSingleItem().Id,
            DocumentLineageTestData.CreateContent("2"),
            Now,
            context).Value;
        lineage.ClearDomainEvents();

        // Act
        var result = lineage.AcknowledgeChanges(revision.Id, Now, context);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Conflict);
        revision.RequiresManualReview.ShouldBeTrue();
        lineage.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void AcknowledgeChanges_conflicts_when_no_manual_review_is_pending()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var context = DocumentAuditTestData.CreateContext();
        var revision = lineage.Revisions.ShouldHaveSingleItem();
        lineage.BeginReview(revision.Id, Now, context).IsSuccess.ShouldBeTrue();
        lineage.ClearDomainEvents();

        // Act
        var result = lineage.AcknowledgeChanges(revision.Id, Now, context);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(DocumentErrors.NoManualReviewPending().ErrorDetails.ShouldNotBeNull().Detail);
        lineage.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void AcknowledgeChanges_reports_a_missing_revision_and_requires_an_audit_context()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var context = DocumentAuditTestData.CreateContext();

        // Act
        var missing = lineage.AcknowledgeChanges(Guid.CreateVersion7(), Now, context);
        var noContext = lineage.AcknowledgeChanges(lineage.Revisions.ShouldHaveSingleItem().Id, Now, null!);

        // Assert
        missing.Status.ShouldBe(ResultStatus.NotFound);
        noContext.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void A_revision_with_requested_changes_keeps_an_acknowledged_review_acknowledged()
    {
        // Arrange
        var lineage = DocumentLineageTestData.Create(now: Now);
        var context = DocumentAuditTestData.CreateContext();
        var revision = lineage.CreateRevision(
            lineage.Revisions.ShouldHaveSingleItem().Id,
            DocumentLineageTestData.CreateContent("2"),
            Now,
            context).Value;
        lineage.BeginReview(revision.Id, Now, context).IsSuccess.ShouldBeTrue();
        lineage.AcknowledgeChanges(revision.Id, Now, context).IsSuccess.ShouldBeTrue();
        lineage.RequestChanges(revision.Id, Now, context).IsSuccess.ShouldBeTrue();
        lineage.BeginReview(revision.Id, Now, context).IsSuccess.ShouldBeTrue();

        // Act
        var approve = lineage.Approve(revision.Id, Now, context);

        // Assert
        approve.IsSuccess.ShouldBeTrue();
    }
}
