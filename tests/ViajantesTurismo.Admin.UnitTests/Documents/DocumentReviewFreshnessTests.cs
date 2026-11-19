using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

public sealed class DocumentReviewFreshnessTests
{
    private static readonly DateTime Now = new(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("1", "SOURCE-VERSION", DocumentReviewFreshnessWarnings.None)]
    [InlineData("1", "CHANGED-SOURCE", DocumentReviewFreshnessWarnings.SourceChanged)]
    [InlineData("2", "SOURCE-VERSION", DocumentReviewFreshnessWarnings.TemplateChanged)]
    [InlineData("1", null, DocumentReviewFreshnessWarnings.SourceUnavailable)]
    [InlineData("2", null, DocumentReviewFreshnessWarnings.SourceUnavailable | DocumentReviewFreshnessWarnings.TemplateChanged)]
    public void A_reviewing_contract_reports_current_freshness_without_changing_its_status(
        string templateVersion,
        string? sourceVersion,
        DocumentReviewFreshnessWarnings expected)
    {
        // Arrange
        var draft = DocumentDraftTestData.Create(Now, templateVersion: templateVersion);
        draft.BeginReview(Now).IsSuccess.ShouldBeTrue();

        // Act
        var warnings = draft.AssessReviewFreshness(Option.FromNullable(sourceVersion));

        // Assert
        draft.CanAssessReviewFreshness.ShouldBeTrue();
        warnings.ShouldBe(expected);
        draft.Status.ShouldBe(DocumentStatus.InReview);
    }

    [Fact]
    public void An_approved_contract_still_reports_staleness_before_finalization()
    {
        // Arrange
        var draft = DocumentDraftTestData.Create(Now);
        draft.BeginReview(Now).IsSuccess.ShouldBeTrue();
        draft.Approve(Now).IsSuccess.ShouldBeTrue();

        // Act
        var warnings = draft.AssessReviewFreshness(Option.Some("CHANGED-SOURCE"));

        // Assert
        draft.CanAssessReviewFreshness.ShouldBeTrue();
        warnings.ShouldBe(DocumentReviewFreshnessWarnings.SourceChanged);
        draft.Status.ShouldBe(DocumentStatus.Approved);
    }

    [Theory]
    [InlineData(DocumentType.BookingConfirmationContract, false)]
    [InlineData(DocumentType.PaymentInstructions, true)]
    public void Review_freshness_is_assessed_only_for_contracts_awaiting_a_decision(
        DocumentType type,
        bool beginReview)
    {
        // Arrange
        var draft = DocumentDraftTestData.Create(Now, type: type);
        if (beginReview)
        {
            draft.BeginReview(Now).IsSuccess.ShouldBeTrue();
        }

        // Act
        var warnings = draft.AssessReviewFreshness(Option.None<string>());

        // Assert
        draft.CanAssessReviewFreshness.ShouldBeFalse();
        warnings.ShouldBe(DocumentReviewFreshnessWarnings.None);
    }
}
