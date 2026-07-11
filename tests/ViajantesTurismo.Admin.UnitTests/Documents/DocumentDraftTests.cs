using SharedKernel.Testing;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, "generated-documents")]
public sealed class DocumentDraftTests
{
    [Fact]
    public void Finalize_seals_artifact_and_preserves_captured_branding()
    {
        // Arrange
        var createdAt = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(createdAt);
        var beginReview = draft.BeginReview(createdAt);
        var approve = draft.Approve(createdAt);
        byte[] artifact = "<html>Viajantes Turismo</html>"u8.ToArray();

        // Act
        var finalize = draft.Finalize(artifact, createdAt);
        artifact[0] = 0;
        var finalizedArtifact = draft.FinalizedArtifactContent;

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.Finalized);
        draft.BrandingVersion.ShouldBe("BRANDING-VERSION");
        draft.BrandingName.ShouldBe("Viajantes Turismo");
        finalizedArtifact.ShouldNotBeNull();
        finalizedArtifact.Value.Span[0].ShouldBe((byte)'<');
        draft.FinalizedArtifactName.ShouldNotContain("customer");
    }

    [Fact]
    public void UpdateField_allows_only_classified_editable_fields()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);

        // Act
        var editableResult = draft.UpdateField("greeting", "Welcome", now);
        var protectedResult = draft.UpdateField("booking-reference", "changed", now);

        // Assert
        editableResult.IsSuccess.ShouldBeTrue();
        protectedResult.IsFailure.ShouldBeTrue();
        draft.Fields.Single(field => field.FieldId == "greeting").RenderedValue.ShouldBe("Welcome");
        draft.Fields.Single(field => field.FieldId == "booking-reference").RenderedValue.ShouldBe("ABC123");
    }

    [Fact]
    public void Void_marks_document_unusable_without_deleting_its_finalized_artifact()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        var beginReview = draft.BeginReview(now);
        var approve = draft.Approve(now);
        var finalize = draft.Finalize("artifact"u8.ToArray(), now);

        // Act
        var result = draft.Void("customer-cancellation", now);

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.Voided);
        draft.FinalizedArtifactContent.ShouldNotBeNull();
    }

    [Fact]
    public void Expired_draft_is_eligible_for_purge_but_finalized_document_is_not()
    {
        // Arrange
        var createdAt = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(createdAt);
        var finalized = DocumentDraftTestData.Create(createdAt);
        var beginReview = finalized.BeginReview(createdAt);
        var approve = finalized.Approve(createdAt);
        var finalize = finalized.Finalize("artifact"u8.ToArray(), createdAt);

        // Act
        var expiredDraft = draft.IsExpiredDraft(createdAt.AddDays(DocumentLimits.DraftRetentionDays));
        var expiredFinalized = finalized.IsExpiredDraft(createdAt.AddYears(DocumentLimits.FinalizedRetentionYears + 1));

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        expiredDraft.ShouldBeTrue();
        expiredFinalized.ShouldBeFalse();
    }
}
