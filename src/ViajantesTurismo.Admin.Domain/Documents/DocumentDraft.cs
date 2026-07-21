using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.InputNormalization;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Represents a versioned, reviewable generated travel document revision.
/// </summary>
public sealed class DocumentDraft : IEntity<Guid>
{
    private readonly List<DocumentField> _fields = [];
    private byte[]? _finalizedArtifactContent;

    private DocumentDraft(
        Guid bookingId,
        DocumentType type,
        DocumentAudience audience,
        string templateId,
        string templateVersion,
        int revision,
        string sourceVersion,
        IEnumerable<DocumentField> fields,
        string brandingVersion,
        string brandingName,
        Uri? brandingLogoUri,
        string brandingPrimaryColor,
        string brandingAccentColor,
        string brandingBackgroundColor,
        string brandingTextColor,
        string brandingHeadingFontFamily,
        string brandingBodyFontFamily,
        string brandingFooterText,
        DateTime createdAt,
        Guid? replacesDocumentId = null,
        Guid? documentLineageId = null)
    {
        Id = Guid.CreateVersion7();
        DocumentLineageId = documentLineageId ?? Guid.CreateVersion7();
        BookingId = bookingId;
        Type = type;
        Audience = audience;
        TemplateId = templateId;
        TemplateVersion = templateVersion;
        Revision = revision;
        SourceVersion = sourceVersion;
        _fields.AddRange(fields);
        BrandingVersion = brandingVersion;
        BrandingName = brandingName;
        BrandingLogoUri = brandingLogoUri;
        BrandingPrimaryColor = brandingPrimaryColor;
        BrandingAccentColor = brandingAccentColor;
        BrandingBackgroundColor = brandingBackgroundColor;
        BrandingTextColor = brandingTextColor;
        BrandingHeadingFontFamily = brandingHeadingFontFamily;
        BrandingBodyFontFamily = brandingBodyFontFamily;
        BrandingFooterText = brandingFooterText;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        RetentionExpiresAt = createdAt.AddDays(DocumentLimits.DraftRetentionDays);
        ReplacesDocumentId = replacesDocumentId;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialisation.
    /// </summary>
    [UsedImplicitly]
    private DocumentDraft()
    {
    }

    /// <summary>Gets the opaque document revision identifier.</summary>
    public Guid Id { get; private init; }

    /// <summary>Gets the owning document lineage identifier.</summary>
    public Guid DocumentLineageId { get; private set; }

    /// <summary>Gets the source booking identifier.</summary>
    public Guid BookingId { get; private init; }

    /// <summary>Gets the document type.</summary>
    public DocumentType Type { get; private init; }

    /// <summary>Gets the intended document audience.</summary>
    public DocumentAudience Audience { get; private init; }

    /// <summary>Gets the deployed template identifier.</summary>
    public string TemplateId { get; private init; } = default!;

    /// <summary>Gets the deployed template version.</summary>
    public string TemplateVersion { get; private init; } = default!;

    /// <summary>Gets the revision number within the document lineage.</summary>
    public int Revision { get; private init; }

    /// <summary>Gets the deterministic source-data version signal.</summary>
    public string SourceVersion { get; private init; } = default!;

    /// <summary>Gets the current workflow status.</summary>
    public DocumentStatus Status { get; private set; } = DocumentStatus.DraftGenerated;

    /// <summary>Gets the immutable captured branding version.</summary>
    public string BrandingVersion { get; private init; } = default!;

    /// <summary>Gets the immutable captured branding display name.</summary>
    public string BrandingName { get; private init; } = default!;

    /// <summary>Gets the immutable captured branding logo URI.</summary>
    public Uri? BrandingLogoUri { get; private init; }

    /// <summary>Gets the immutable captured primary brand color.</summary>
    public string BrandingPrimaryColor { get; private init; } = default!;

    /// <summary>Gets the immutable captured accent brand color.</summary>
    public string BrandingAccentColor { get; private init; } = default!;

    /// <summary>Gets the immutable captured document background color.</summary>
    public string BrandingBackgroundColor { get; private init; } = default!;

    /// <summary>Gets the immutable captured document text color.</summary>
    public string BrandingTextColor { get; private init; } = default!;

    /// <summary>Gets the immutable captured heading font family.</summary>
    public string BrandingHeadingFontFamily { get; private init; } = default!;

    /// <summary>Gets the immutable captured body font family.</summary>
    public string BrandingBodyFontFamily { get; private init; } = default!;

    /// <summary>Gets the immutable captured footer text.</summary>
    public string BrandingFooterText { get; private init; } = default!;

    /// <summary>Gets the classified document fields.</summary>
    public IReadOnlyList<DocumentField> Fields => _fields.AsReadOnly();

    /// <summary>Gets when this revision was created.</summary>
    public DateTime CreatedAt { get; private init; }

    /// <summary>Gets when this revision was last changed.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Gets when this revision may be removed under retention policy.</summary>
    public DateTime? RetentionExpiresAt { get; private set; }

    /// <summary>Gets when this revision was finalized.</summary>
    public DateTime? FinalizedAt { get; private set; }

    /// <summary>Gets the opaque finalized artifact name.</summary>
    public string? FinalizedArtifactName { get; private set; }

    /// <summary>Gets the preceding revision replaced by this revision, when applicable.</summary>
    public Guid? ReplacesDocumentId { get; private init; }

    /// <summary>Gets the reason recorded when the document was voided.</summary>
    public string? VoidReason { get; private set; }

    /// <summary>Gets a copy of the finalized artifact content.</summary>
    public ReadOnlyMemory<byte>? GetFinalizedArtifactContent()
    {
        if (_finalizedArtifactContent is null)
        {
            return null;
        }

        return new ReadOnlyMemory<byte>(_finalizedArtifactContent.ToArray());
    }

    /// <summary>Creates the initial document draft revision.</summary>
    internal static Result<DocumentDraft> Create(
        Guid bookingId,
        DocumentType type,
        DocumentAudience audience,
        string templateId,
        string templateVersion,
        string sourceVersion,
        IEnumerable<DocumentField> fields,
        string brandingVersion,
        string brandingName,
        Uri? brandingLogoUri,
        DateTime createdAt) => Create(
            bookingId,
            type,
            audience,
            templateId,
            templateVersion,
            sourceVersion,
            fields,
            brandingVersion,
            brandingName,
            brandingLogoUri,
            "#000000",
            "#000000",
            "#ffffff",
            "#000000",
            "system-ui, sans-serif",
            "system-ui, sans-serif",
            brandingName,
            createdAt);

    /// <summary>Creates the initial document draft revision.</summary>
    internal static Result<DocumentDraft> Create(
        Guid bookingId,
        DocumentType type,
        DocumentAudience audience,
        string templateId,
        string templateVersion,
        string sourceVersion,
        IEnumerable<DocumentField> fields,
        string brandingVersion,
        string brandingName,
        Uri? brandingLogoUri,
        string brandingPrimaryColor,
        string brandingAccentColor,
        string brandingBackgroundColor,
        string brandingTextColor,
        string brandingHeadingFontFamily,
        string brandingBodyFontFamily,
        string brandingFooterText,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var fieldList = fields.ToList();
        var validation = Validate(
            bookingId,
            type,
            audience,
            templateId,
            templateVersion,
            sourceVersion,
            fieldList,
            brandingVersion,
            brandingName,
            brandingLogoUri,
            brandingPrimaryColor,
            brandingAccentColor,
            brandingBackgroundColor,
            brandingTextColor,
            brandingHeadingFontFamily,
            brandingBodyFontFamily,
            brandingFooterText);
        if (validation.IsFailure)
        {
            return validation.ConvertError<DocumentDraft>();
        }

        var ownedFields = fieldList
            .Select((field, index) => field.CopyForOwnership(index))
            .ToList();
        return Result.Ok(new DocumentDraft(
            bookingId,
            type,
            audience,
            templateId,
            templateVersion,
            1,
            sourceVersion,
            ownedFields,
            brandingVersion,
            brandingName,
            brandingLogoUri,
            brandingPrimaryColor,
            brandingAccentColor,
            brandingBackgroundColor,
            brandingTextColor,
            brandingHeadingFontFamily,
            brandingBodyFontFamily,
            brandingFooterText,
            createdAt));
    }

    /// <summary>Creates a replacement draft with refreshed source data.</summary>
    internal Result<DocumentDraft> CreateRevision(
        string templateId,
        string templateVersion,
        string sourceVersion,
        IEnumerable<DocumentField> fields,
        string brandingVersion,
        string brandingName,
        Uri? brandingLogoUri,
        DateTime createdAt) => CreateRevision(
            templateId,
            templateVersion,
            sourceVersion,
            fields,
            brandingVersion,
            brandingName,
            brandingLogoUri,
            "#000000",
            "#000000",
            "#ffffff",
            "#000000",
            "system-ui, sans-serif",
            "system-ui, sans-serif",
            brandingName,
            createdAt);

    /// <summary>Creates a replacement draft with refreshed source data.</summary>
    internal Result<DocumentDraft> CreateRevision(
        string templateId,
        string templateVersion,
        string sourceVersion,
        IEnumerable<DocumentField> fields,
        string brandingVersion,
        string brandingName,
        Uri? brandingLogoUri,
        string brandingPrimaryColor,
        string brandingAccentColor,
        string brandingBackgroundColor,
        string brandingTextColor,
        string brandingHeadingFontFamily,
        string brandingBodyFontFamily,
        string brandingFooterText,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var fieldList = fields.ToList();
        var validation = Validate(
            BookingId,
            Type,
            Audience,
            templateId,
            templateVersion,
            sourceVersion,
            fieldList,
            brandingVersion,
            brandingName,
            brandingLogoUri,
            brandingPrimaryColor,
            brandingAccentColor,
            brandingBackgroundColor,
            brandingTextColor,
            brandingHeadingFontFamily,
            brandingBodyFontFamily,
            brandingFooterText);
        if (validation.IsFailure)
        {
            return validation.ConvertError<DocumentDraft>();
        }

        var ownedFields = fieldList
            .Select((field, index) => field.CopyForOwnership(
                index,
                _fields.FirstOrDefault(previous => previous.FieldId == field.FieldId)))
            .ToList();
        return Result.Ok(new DocumentDraft(
            BookingId,
            Type,
            Audience,
            templateId,
            templateVersion,
            Revision + 1,
            sourceVersion,
            ownedFields,
            brandingVersion,
            brandingName,
            brandingLogoUri,
            brandingPrimaryColor,
            brandingAccentColor,
            brandingBackgroundColor,
            brandingTextColor,
            brandingHeadingFontFamily,
            brandingBodyFontFamily,
            brandingFooterText,
            createdAt,
            Id,
            DocumentLineageId));
    }

    internal static Result<DocumentDraft> CreateForLineage(
        Guid documentLineageId,
        Guid bookingId,
        DocumentType type,
        DocumentAudience audience,
        DocumentDraftContent content,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(content);
        var result = Create(
            bookingId,
            type,
            audience,
            content.TemplateId,
            content.TemplateVersion,
            content.SourceVersion,
            content.Fields,
            content.BrandingVersion,
            content.BrandingName,
            content.BrandingLogoUri,
            content.BrandingPrimaryColor,
            content.BrandingAccentColor,
            content.BrandingBackgroundColor,
            content.BrandingTextColor,
            content.BrandingHeadingFontFamily,
            content.BrandingBodyFontFamily,
            content.BrandingFooterText,
            createdAt);
        if (result.IsSuccess)
        {
            result.Value.DocumentLineageId = documentLineageId;
        }

        return result;
    }

    internal Result<DocumentDraft> CreateReplacement(
        DocumentDraftContent content,
        int revision,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(content);
        var fieldList = content.Fields.ToList();
        var validation = Validate(
            BookingId,
            Type,
            Audience,
            content.TemplateId,
            content.TemplateVersion,
            content.SourceVersion,
            fieldList,
            content.BrandingVersion,
            content.BrandingName,
            content.BrandingLogoUri,
            content.BrandingPrimaryColor,
            content.BrandingAccentColor,
            content.BrandingBackgroundColor,
            content.BrandingTextColor,
            content.BrandingHeadingFontFamily,
            content.BrandingBodyFontFamily,
            content.BrandingFooterText);
        if (validation.IsFailure)
        {
            return validation.ConvertError<DocumentDraft>();
        }

        var ownedFields = fieldList
            .Select((field, index) => field.CopyForOwnership(
                index,
                _fields.FirstOrDefault(previous => previous.FieldId == field.FieldId)))
            .ToList();
        return Result.Ok(new DocumentDraft(
            BookingId,
            Type,
            Audience,
            content.TemplateId,
            content.TemplateVersion,
            revision,
            content.SourceVersion,
            ownedFields,
            content.BrandingVersion,
            content.BrandingName,
            content.BrandingLogoUri,
            content.BrandingPrimaryColor,
            content.BrandingAccentColor,
            content.BrandingBackgroundColor,
            content.BrandingTextColor,
            content.BrandingHeadingFontFamily,
            content.BrandingBodyFontFamily,
            content.BrandingFooterText,
            createdAt,
            Id,
            DocumentLineageId));
    }

    /// <summary>Starts or restarts staff review.</summary>
    internal Result BeginReview(DateTime now)
    {
        if (Status is not (DocumentStatus.DraftGenerated or DocumentStatus.ChangesRequested))
        {
            return DocumentErrors.InvalidStatusTransition(Status, DocumentStatus.InReview);
        }

        Status = DocumentStatus.InReview;
        UpdatedAt = now;
        return Result.Ok();
    }

    /// <summary>Records requested changes.</summary>
    internal Result RequestChanges(DateTime now)
    {
        if (Status is not (DocumentStatus.InReview or DocumentStatus.Approved))
        {
            return DocumentErrors.InvalidStatusTransition(Status, DocumentStatus.ChangesRequested);
        }

        Status = DocumentStatus.ChangesRequested;
        UpdatedAt = now;
        return Result.Ok();
    }

    /// <summary>Updates a staff-editable field.</summary>
    internal Result UpdateField(string fieldId, string value, DateTime now)
    {
        if (Status is DocumentStatus.Finalized or DocumentStatus.Superseded or DocumentStatus.Voided)
        {
            return DocumentErrors.DocumentIsImmutable(Status);
        }

        var field = _fields.FirstOrDefault(item => item.FieldId == fieldId);
        if (field is null)
        {
            return DocumentErrors.FieldNotFound(fieldId);
        }

        var result = field.SetStaffOverride(value);
        if (result.IsFailure)
        {
            return result;
        }

        Status = DocumentStatus.InReview;
        UpdatedAt = now;
        return Result.Ok();
    }

    /// <summary>Approves a document under active staff review.</summary>
    internal Result Approve(DateTime now)
    {
        if (Status != DocumentStatus.InReview)
        {
            return DocumentErrors.InvalidStatusTransition(Status, DocumentStatus.Approved);
        }

        Status = DocumentStatus.Approved;
        UpdatedAt = now;
        return Result.Ok();
    }

    /// <summary>Seals the deterministic final artifact for an approved revision.</summary>
    internal Result Finalize(byte[] artifactContent, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(artifactContent);

        if (Status != DocumentStatus.Approved)
        {
            return DocumentErrors.InvalidStatusTransition(Status, DocumentStatus.Finalized);
        }

        if (artifactContent.Length == 0)
        {
            return DocumentErrors.ArtifactContentRequired();
        }

        _finalizedArtifactContent = artifactContent.ToArray();
        FinalizedArtifactName = $"document-{Id:N}-r{Revision}.html";
        FinalizedAt = now;
        RetentionExpiresAt = null;
        Status = DocumentStatus.Finalized;
        UpdatedAt = now;
        return Result.Ok();
    }

    /// <summary>Marks a finalized revision as replaced by a newer finalized revision.</summary>
    internal Result Supersede(DateTime now)
    {
        if (Status != DocumentStatus.Finalized)
        {
            return DocumentErrors.InvalidStatusTransition(Status, DocumentStatus.Superseded);
        }

        Status = DocumentStatus.Superseded;
        UpdatedAt = now;
        return Result.Ok();
    }

    /// <summary>Voids a document with a non-empty reason.</summary>
    internal Result Void(string reason, DateTime now)
    {
        if (Status != DocumentStatus.Finalized)
        {
            return DocumentErrors.InvalidStatusTransition(Status, DocumentStatus.Voided);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return DocumentErrors.VoidReasonRequired();
        }

        if (reason.Length > DocumentLimits.MaxVoidReasonLength)
        {
            return DocumentErrors.ValueTooLong("reason", DocumentLimits.MaxVoidReasonLength);
        }

        VoidReason = reason;
        Status = DocumentStatus.Voided;
        UpdatedAt = now;
        return Result.Ok();
    }

    /// <summary>Gets whether this unfinalized draft is eligible for purge.</summary>
    public bool IsExpiredDraft(DateTime now) =>
        FinalizedAt is null
        && RetentionExpiresAt is { } retentionExpiresAt
        && retentionExpiresAt <= now;

    private static Result Validate(
        Guid bookingId,
        DocumentType type,
        DocumentAudience audience,
        string templateId,
        string templateVersion,
        string sourceVersion,
        List<DocumentField> fields,
        string brandingVersion,
        string brandingName,
        Uri? brandingLogoUri,
        string brandingPrimaryColor,
        string brandingAccentColor,
        string brandingBackgroundColor,
        string brandingTextColor,
        string brandingHeadingFontFamily,
        string brandingBodyFontFamily,
        string brandingFooterText)
    {
        var metadataValidation = ValidateDocumentMetadata(
            bookingId,
            type,
            audience,
            templateId,
            templateVersion,
            sourceVersion,
            brandingVersion,
            brandingName,
            brandingLogoUri);
        if (metadataValidation.IsFailure)
        {
            return metadataValidation;
        }

        var brandingTokenValidation = ValidateBrandingTokens(
            brandingPrimaryColor,
            brandingAccentColor,
            brandingBackgroundColor,
            brandingTextColor,
            brandingHeadingFontFamily,
            brandingBodyFontFamily,
            brandingFooterText);
        if (brandingTokenValidation.IsFailure)
        {
            return brandingTokenValidation;
        }

        return ValidateFields(fields);
    }

    private static Result ValidateDocumentMetadata(
        Guid bookingId,
        DocumentType type,
        DocumentAudience audience,
        string templateId,
        string templateVersion,
        string sourceVersion,
        string brandingVersion,
        string brandingName,
        Uri? brandingLogoUri)
    {
        if (bookingId == Guid.Empty)
        {
            return DocumentErrors.ValueRequired("bookingId");
        }

        if (!Enum.IsDefined(type))
        {
            return DocumentErrors.InvalidValue("documentType");
        }

        if (!Enum.IsDefined(audience))
        {
            return DocumentErrors.InvalidValue("documentAudience");
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            return DocumentErrors.ValueRequired("templateId");
        }

        if (templateId.Length > DocumentLimits.MaxTemplateIdLength)
        {
            return DocumentErrors.ValueTooLong("templateId", DocumentLimits.MaxTemplateIdLength);
        }

        if (string.IsNullOrWhiteSpace(templateVersion))
        {
            return DocumentErrors.ValueRequired("templateVersion");
        }

        if (templateVersion.Length > DocumentLimits.MaxTemplateVersionLength)
        {
            return DocumentErrors.ValueTooLong("templateVersion", DocumentLimits.MaxTemplateVersionLength);
        }

        if (string.IsNullOrWhiteSpace(sourceVersion))
        {
            return DocumentErrors.ValueRequired("sourceVersion");
        }

        if (sourceVersion.Length > DocumentLimits.MaxSourceVersionLength)
        {
            return DocumentErrors.ValueTooLong("sourceVersion", DocumentLimits.MaxSourceVersionLength);
        }

        if (string.IsNullOrWhiteSpace(brandingVersion))
        {
            return DocumentErrors.ValueRequired("brandingVersion");
        }

        if (brandingVersion.Length > DocumentLimits.MaxBrandingVersionLength)
        {
            return DocumentErrors.ValueTooLong("brandingVersion", DocumentLimits.MaxBrandingVersionLength);
        }

        if (string.IsNullOrWhiteSpace(brandingName))
        {
            return DocumentErrors.ValueRequired("brandingName");
        }

        if (brandingName.Length > DocumentLimits.MaxBrandingNameLength)
        {
            return DocumentErrors.ValueTooLong("brandingName", DocumentLimits.MaxBrandingNameLength);
        }

        return ValidateBrandingLogoUri(brandingLogoUri);
    }

    private static Result ValidateBrandingTokens(
        string brandingPrimaryColor,
        string brandingAccentColor,
        string brandingBackgroundColor,
        string brandingTextColor,
        string brandingHeadingFontFamily,
        string brandingBodyFontFamily,
        string brandingFooterText)
    {
        foreach (var (field, value) in new[]
        {
            ("brandingPrimaryColor", brandingPrimaryColor),
            ("brandingAccentColor", brandingAccentColor),
            ("brandingBackgroundColor", brandingBackgroundColor),
            ("brandingTextColor", brandingTextColor),
            ("brandingHeadingFontFamily", brandingHeadingFontFamily),
            ("brandingBodyFontFamily", brandingBodyFontFamily),
            ("brandingFooterText", brandingFooterText),
        })
        {
            var validation = ValidateBrandingToken(field, value);
            if (validation.IsFailure)
            {
                return validation;
            }
        }

        return Result.Ok();
    }

    private static Result ValidateFields(List<DocumentField> fields)
    {
        var duplicateField = fields
            .GroupBy(field => field.FieldId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Skip(1).Any());
        if (duplicateField is not null)
        {
            return DocumentErrors.DuplicateFieldId(duplicateField.Key);
        }

        if (fields.Count == 0)
        {
            return DocumentErrors.ValueRequired("fields");
        }

        var unclassified = fields.FirstOrDefault(field => field.PrivacyClassification == DocumentPrivacyClassification.Unclassified);
        if (unclassified is not null)
        {
            return DocumentErrors.UnclassifiedField(unclassified.FieldId);
        }

        return Result.Ok();
    }

    private static Result ValidateBrandingToken(string field, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DocumentErrors.ValueRequired(field);
        }

        return value.Length > DocumentLimits.MaxBrandingTokenLength
            ? DocumentErrors.ValueTooLong(field, DocumentLimits.MaxBrandingTokenLength)
            : Result.Ok();
    }

    private static Result ValidateBrandingLogoUri(Uri? brandingLogoUri)
    {
        if (brandingLogoUri?.OriginalString.Length > DocumentLimits.MaxBrandingLogoUriLength)
        {
            return DocumentErrors.ValueTooLong("brandingLogoUri", DocumentLimits.MaxBrandingLogoUriLength);
        }

        return brandingLogoUri is not null &&
            WebAssetUriSanitizer.NormalizeRootRelativeOrHttps(brandingLogoUri.OriginalString, DocumentLimits.MaxBrandingLogoUriLength) is null
                ? DocumentErrors.InvalidValue("brandingLogoUri")
                : Result.Ok();
    }

}
