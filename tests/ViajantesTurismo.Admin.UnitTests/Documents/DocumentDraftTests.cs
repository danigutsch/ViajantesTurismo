using System.Runtime.InteropServices;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
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
        var finalizedArtifact = draft.CopyFinalizedArtifactContent();
        var secondFinalizedArtifact = draft.CopyFinalizedArtifactContent();

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.Finalized);
        draft.BrandingVersion.ShouldBe("BRANDING-VERSION");
        draft.BrandingName.ShouldBe("Viajantes Turismo");
        var artifactContent = finalizedArtifact.ShouldNotBeNull();
        artifactContent.Span[0].ShouldBe((byte)'<');
        var hasExposedArray = MemoryMarshal.TryGetArray(artifactContent, out var segment);
        if (hasExposedArray)
        {
            var exposedArray = segment.Array.ShouldNotBeNull();
            exposedArray[segment.Offset] = 0;
            secondFinalizedArtifact.ShouldNotBeNull().Span[0].ShouldBe((byte)'<');
        }

        draft.FinalizedArtifactName.ShouldNotContain("customer", StringComparison.Ordinal);
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
        draft.Fields.ShouldHaveSingleItem(field => field.FieldId == "greeting").RenderedValue.ShouldBe("Welcome");
        draft.Fields.ShouldHaveSingleItem(field => field.FieldId == "booking-reference").RenderedValue.ShouldBe("ABC123");
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
        var result = draft.Void(DocumentVoidReason.AdministrativeVoid, now);

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.Voided);
        draft.CopyFinalizedArtifactContent().ShouldNotBeNull();
    }

    [Fact]
    public void Void_rejects_a_draft_without_a_finalized_artifact()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);

        // Act
        var result = draft.Void(DocumentVoidReason.AdministrativeVoid, now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.DraftGenerated);
    }

    [Fact]
    public void RequestChanges_reopens_an_approved_document_for_review()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        var beginReview = draft.BeginReview(now);
        var approve = draft.Approve(now);

        // Act
        var result = draft.RequestChanges(now.AddMinutes(1));

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.ChangesRequested);
        draft.UpdatedAt.ShouldBe(now.AddMinutes(1));
    }

    [Fact]
    public void RequestChanges_rejects_documents_that_are_not_under_review_or_approved()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);

        // Act
        var result = draft.RequestChanges(now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.DraftGenerated);
    }

    [Fact]
    public void BeginReview_rejects_finalized_documents()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        var beginReview = draft.BeginReview(now);
        var approve = draft.Approve(now);
        var finalize = draft.Finalize("artifact"u8.ToArray(), now);

        // Act
        var result = draft.BeginReview(now.AddMinutes(1));

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.Finalized);
    }

    [Fact]
    public void UpdateField_rejects_missing_fields_and_finalized_documents()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        var finalized = DocumentDraftTestData.Create(now);
        var beginReview = finalized.BeginReview(now);
        var approve = finalized.Approve(now);
        var finalize = finalized.Finalize("artifact"u8.ToArray(), now);

        // Act
        var missingFieldResult = draft.UpdateField("missing-field", "value", now);
        var immutableResult = finalized.UpdateField("greeting", "value", now.AddMinutes(1));

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        missingFieldResult.IsFailure.ShouldBeTrue();
        immutableResult.IsFailure.ShouldBeTrue();
        finalized.Fields.ShouldHaveSingleItem(field => field.FieldId == "greeting").RenderedValue.ShouldBe("Dear customer");
    }

    [Fact]
    public void Approve_rejects_documents_that_are_not_in_review()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);

        // Act
        var result = draft.Approve(now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.DraftGenerated);
    }

    [Fact]
    public void Finalize_rejects_empty_artifacts()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        var beginReview = draft.BeginReview(now);
        var approve = draft.Approve(now);

        // Act
        var result = draft.Finalize([], now);

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.Approved);
        draft.CopyFinalizedArtifactContent().ShouldBeNull();
    }

    [Fact]
    public void Supersede_marks_finalized_documents_as_replaced()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        var beginReview = draft.BeginReview(now);
        var approve = draft.Approve(now);
        var finalize = draft.Finalize("artifact"u8.ToArray(), now);

        // Act
        var result = draft.Supersede(now.AddMinutes(1));

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.Superseded);
        draft.UpdatedAt.ShouldBe(now.AddMinutes(1));
    }

    [Fact]
    public void Supersede_rejects_documents_that_are_not_finalized()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);

        // Act
        var result = draft.Supersede(now);

        // Assert
        result.IsFailure.ShouldBeTrue();
        draft.Status.ShouldBe(DocumentStatus.DraftGenerated);
    }

    [Fact]
    public void Void_rejects_undefined_and_legacy_reasons()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var undefinedReasonDraft = DocumentDraftTestData.Create(now);
        var legacyReasonDraft = DocumentDraftTestData.Create(now);
        var undefinedBeginReview = undefinedReasonDraft.BeginReview(now);
        var undefinedApprove = undefinedReasonDraft.Approve(now);
        var undefinedFinalize = undefinedReasonDraft.Finalize("artifact"u8.ToArray(), now);
        var legacyBeginReview = legacyReasonDraft.BeginReview(now);
        var legacyApprove = legacyReasonDraft.Approve(now);
        var legacyFinalize = legacyReasonDraft.Finalize("artifact"u8.ToArray(), now);

        // Act
        var undefinedReasonResult = undefinedReasonDraft.Void((DocumentVoidReason)99, now.AddMinutes(1));
        var legacyReasonResult = legacyReasonDraft.Void(DocumentVoidReason.Legacy, now.AddMinutes(1));

        // Assert
        undefinedBeginReview.IsSuccess.ShouldBeTrue();
        undefinedApprove.IsSuccess.ShouldBeTrue();
        undefinedFinalize.IsSuccess.ShouldBeTrue();
        legacyBeginReview.IsSuccess.ShouldBeTrue();
        legacyApprove.IsSuccess.ShouldBeTrue();
        legacyFinalize.IsSuccess.ShouldBeTrue();
        undefinedReasonResult.IsFailure.ShouldBeTrue();
        legacyReasonResult.IsFailure.ShouldBeTrue();
        undefinedReasonDraft.Status.ShouldBe(DocumentStatus.Finalized);
        legacyReasonDraft.Status.ShouldBe(DocumentStatus.Finalized);
    }

    [Fact]
    public void Create_rejects_source_and_branding_values_that_exceed_persistence_limits()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        DocumentField[] fields = [DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value];

        // Act
        var sourceVersionResult = DocumentDraft.Create(
            Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            "tour-service-contract", "1", new string('s', 65), fields, "BRANDING-VERSION", "Viajantes Turismo", null, now);
        var brandingVersionResult = DocumentDraft.Create(
            Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            "tour-service-contract", "1", "SOURCE-VERSION", fields, new string('b', 65), "Viajantes Turismo", null, now);
        var brandingNameResult = DocumentDraft.Create(
            Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            "tour-service-contract", "1", "SOURCE-VERSION", fields, "BRANDING-VERSION", new string('n', 129), null, now);
        var brandingLogoResult = DocumentDraft.Create(
            Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            "tour-service-contract", "1", "SOURCE-VERSION", fields, "BRANDING-VERSION", "Viajantes Turismo",
            new Uri($"/{new string('l', DocumentLimits.MaxBrandingLogoUriLength)}", UriKind.Relative), now);

        // Assert
        sourceVersionResult.IsFailure.ShouldBeTrue();
        brandingVersionResult.IsFailure.ShouldBeTrue();
        brandingNameResult.IsFailure.ShouldBeTrue();
        brandingLogoResult.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("bookingId")]
    [InlineData("templateId")]
    [InlineData("templateVersion")]
    [InlineData("sourceVersion")]
    [InlineData("brandingVersion")]
    [InlineData("brandingName")]
    public void Create_rejects_missing_required_values(string missingField)
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        DocumentField[] fields = [DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value];

        // Act
        var result = missingField switch
        {
            "bookingId" => DocumentDraft.Create(
                Guid.Empty, DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
                "tour-service-contract", "1", "SOURCE-VERSION", fields, "BRANDING-VERSION", "Viajantes Turismo", null, now),
            "templateId" => DocumentDraft.Create(
                Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
                " ", "1", "SOURCE-VERSION", fields, "BRANDING-VERSION", "Viajantes Turismo", null, now),
            "templateVersion" => DocumentDraft.Create(
                Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
                "tour-service-contract", " ", "SOURCE-VERSION", fields, "BRANDING-VERSION", "Viajantes Turismo", null, now),
            "sourceVersion" => DocumentDraft.Create(
                Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
                "tour-service-contract", "1", " ", fields, "BRANDING-VERSION", "Viajantes Turismo", null, now),
            "brandingVersion" => DocumentDraft.Create(
                Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
                "tour-service-contract", "1", "SOURCE-VERSION", fields, " ", "Viajantes Turismo", null, now),
            _ => DocumentDraft.Create(
                Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
                "tour-service-contract", "1", "SOURCE-VERSION", fields, "BRANDING-VERSION", " ", null, now),
        };

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_template_values_that_exceed_persistence_limits()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        DocumentField[] fields = [DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value];

        // Act
        var templateIdResult = DocumentDraft.Create(
            Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            new string('t', DocumentLimits.MaxTemplateIdLength + 1), "1", "SOURCE-VERSION", fields, "BRANDING-VERSION", "Viajantes Turismo", null, now);
        var templateVersionResult = DocumentDraft.Create(
            Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            "tour-service-contract", new string('v', DocumentLimits.MaxTemplateVersionLength + 1), "SOURCE-VERSION", fields, "BRANDING-VERSION", "Viajantes Turismo", null, now);

        // Assert
        templateIdResult.IsFailure.ShouldBeTrue();
        templateVersionResult.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("brandingPrimaryColor")]
    [InlineData("brandingAccentColor")]
    [InlineData("brandingBackgroundColor")]
    [InlineData("brandingTextColor")]
    [InlineData("brandingHeadingFontFamily")]
    [InlineData("brandingBodyFontFamily")]
    [InlineData("brandingFooterText")]
    public void Create_rejects_missing_branding_tokens(string missingToken)
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        DocumentField[] fields = [DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value];
        var primaryColor = missingToken == "brandingPrimaryColor" ? " " : "#102030";
        var accentColor = missingToken == "brandingAccentColor" ? " " : "#405060";
        var backgroundColor = missingToken == "brandingBackgroundColor" ? " " : "#ffffff";
        var textColor = missingToken == "brandingTextColor" ? " " : "#111111";
        var headingFont = missingToken == "brandingHeadingFontFamily" ? " " : "Montserrat";
        var bodyFont = missingToken == "brandingBodyFontFamily" ? " " : "Inter";
        var footerText = missingToken == "brandingFooterText" ? " " : "Legal footer";

        // Act
        var result = DocumentDraft.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            "tour-service-contract",
            "1",
            "SOURCE-VERSION",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            null,
            primaryColor,
            accentColor,
            backgroundColor,
            textColor,
            headingFont,
            bodyFont,
            footerText,
            now);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_branding_tokens_that_exceed_persistence_limits()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        DocumentField[] fields = [DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value];

        // Act
        var result = DocumentDraft.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            "tour-service-contract",
            "1",
            "SOURCE-VERSION",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            null,
            new string('c', DocumentLimits.MaxBrandingTokenLength + 1),
            "#405060",
            "#ffffff",
            "#111111",
            "Montserrat",
            "Inter",
            "Legal footer",
            now);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_missing_fields()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var emptyFieldsResult = DocumentDraft.Create(
            Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            "tour-service-contract", "1", "SOURCE-VERSION", [], "BRANDING-VERSION", "Viajantes Turismo", null, now);

        // Assert
        emptyFieldsResult.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_accepts_safe_https_branding_logo_and_custom_tokens()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        DocumentField[] fields = [DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value];
        var logoUri = new Uri("https://cdn.example.test/logo.svg", UriKind.Absolute);

        // Act
        var result = DocumentDraft.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            "tour-service-contract",
            "1",
            "SOURCE-VERSION",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            logoUri,
            "#102030",
            "#405060",
            "#ffffff",
            "#111111",
            "Montserrat",
            "Inter",
            "Legal footer",
            now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.BrandingLogoUri.ShouldBe(logoUri);
        result.Value.BrandingFooterText.ShouldBe("Legal footer");
    }

    [Fact]
    public void Create_rejects_invalid_enum_values_and_duplicate_field_identifiers()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        DocumentField[] duplicateFields =
        [
            DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
            DocumentField.Create("greeting", "Greeting again", "Welcome", DocumentPrivacyClassification.PersonalData, true).Value,
        ];

        // Act
        var invalidTypeResult = DocumentDraft.Create(
            Guid.CreateVersion7(), (DocumentType)99, DocumentAudience.Customer,
            "tour-service-contract", "1", "SOURCE-VERSION", duplicateFields.Take(1), "BRANDING-VERSION", "Viajantes Turismo", null, now);
        var invalidAudienceResult = DocumentDraft.Create(
            Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, (DocumentAudience)99,
            "tour-service-contract", "1", "SOURCE-VERSION", duplicateFields.Take(1), "BRANDING-VERSION", "Viajantes Turismo", null, now);
        var duplicateFieldsResult = DocumentDraft.Create(
            Guid.CreateVersion7(), DocumentType.BookingConfirmationContract, DocumentAudience.Customer,
            "tour-service-contract", "1", "SOURCE-VERSION", duplicateFields, "BRANDING-VERSION", "Viajantes Turismo", null, now);

        // Assert
        invalidTypeResult.IsFailure.ShouldBeTrue();
        invalidAudienceResult.IsFailure.ShouldBeTrue();
        duplicateFieldsResult.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_field_rejects_secret_content()
    {
        // Arrange
        const string value = "credential";

        // Act
        var result = DocumentField.Create("secret", "Secret", value, DocumentPrivacyClassification.Secret, false);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_field_rejects_undefined_privacy_classification()
    {
        // Act
        var result = DocumentField.Create(
            "greeting",
            "Greeting",
            "Dear customer",
            (DocumentPrivacyClassification)999,
            true);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_field_accepts_generated_value_above_the_staff_override_limit()
    {
        // Arrange
        var generatedValue = new string('x', 4_001);

        // Act
        var result = DocumentField.Create(
            "included-services",
            "Included services",
            generatedValue,
            DocumentPrivacyClassification.Operational,
            false);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(generatedValue);
    }

    [Fact]
    public void CreateRevision_discards_override_when_the_generated_value_changes()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        var update = draft.UpdateField("greeting", "Dear Ada,", now);
        DocumentField[] fields =
        [
            DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
            DocumentField.Create("greeting", "Greeting", "Dear Bea,", DocumentPrivacyClassification.PersonalData, true).Value,
        ];

        // Act
        var revision = draft.CreateRevision("tour-service-contract", "2", "SOURCE-VERSION-2", fields, "BRANDING-VERSION", "Viajantes Turismo", null, now);

        // Assert
        update.IsSuccess.ShouldBeTrue();
        revision.IsSuccess.ShouldBeTrue();
        revision.Value.Fields.ShouldHaveSingleItem(field => field.FieldId == "greeting").RenderedValue.ShouldBe("Dear Bea,");
    }

    [Fact]
    public void CreateRevision_rejects_branding_logo_values_that_exceed_persistence_limits()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        DocumentField[] fields =
        [
            DocumentField.Create("booking-reference", "Booking reference", "ABC123", DocumentPrivacyClassification.Operational, false).Value,
            DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value,
        ];

        // Act
        var revision = draft.CreateRevision(
            "tour-service-contract",
            "2",
            "SOURCE-VERSION-2",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            new Uri($"/{new string('l', DocumentLimits.MaxBrandingLogoUriLength)}", UriKind.Relative),
            now);

        // Assert
        revision.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("http://example.test/logo.svg")]
    [InlineData("https://viewer@example.test/logo.svg")]
    [InlineData("//example.test/logo.svg")]
    [InlineData("/\\evil.test/logo.svg")]
    public void Create_rejects_unsafe_branding_logo_uris(string logoValue)
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        DocumentField[] fields = [DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value];

        // Act
        var result = DocumentDraft.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            "tour-service-contract",
            "1",
            "SOURCE-VERSION",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            new Uri(logoValue, UriKind.RelativeOrAbsolute),
            now);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void CreateRevision_rejects_unsafe_branding_logo_uris()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        DocumentField[] fields = [DocumentField.Create("greeting", "Greeting", "Dear customer", DocumentPrivacyClassification.PersonalData, true).Value];

        // Act
        var result = draft.CreateRevision(
            "tour-service-contract",
            "2",
            "SOURCE-VERSION-2",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            new Uri("/\\evil.test/logo.svg", UriKind.Relative),
            now);

        // Assert
        result.IsFailure.ShouldBeTrue();
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
        var expiredFinalized = finalized.IsExpiredDraft(createdAt.AddYears(1));

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        expiredDraft.ShouldBeTrue();
        expiredFinalized.ShouldBeFalse();
        finalized.RetentionExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void Finalized_artifact_is_available_only_after_finalization()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var draft = DocumentDraftTestData.Create(now);
        var beginReview = draft.BeginReview(now);
        var approve = draft.Approve(now);
        var availableBeforeFinalization = draft.HasAvailableFinalizedArtifact;

        // Act
        var finalize = draft.Finalize("artifact"u8.ToArray(), now);

        // Assert
        beginReview.IsSuccess.ShouldBeTrue();
        approve.IsSuccess.ShouldBeTrue();
        finalize.IsSuccess.ShouldBeTrue();
        availableBeforeFinalization.ShouldBeFalse();
        draft.HasAvailableFinalizedArtifact.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DocumentType.PaymentInstructions, true)]
    [InlineData(DocumentType.BookingConfirmationContract, false)]
    public void Only_payment_instructions_require_the_payment_instruction_workflow(DocumentType type, bool expected)
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var draft = DocumentDraftTestData.Create(now, type: type);

        // Assert
        draft.RequiresPaymentInstructionWorkflow.ShouldBe(expected);
    }
}
