using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Contracts;
using SharedKernel.InputNormalization;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Domain.Media;

/// <summary>
/// Public metadata for a media image used by Catalog tours.
/// </summary>
public sealed class PublicMediaImage
{
    private readonly List<MediaImageResponsiveVariant> _responsiveVariants = [];
    private readonly List<MediaImageTourLink> _tourLinks = [];
    private readonly List<string> _tags = [];
    private readonly List<PublicMediaImageAccessibilityText> _accessibilityTexts = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicMediaImage"/> class.
    /// </summary>
    /// <param name="metadata">The scalar media image metadata.</param>
    /// <param name="responsiveVariants">The public responsive renditions.</param>
    /// <param name="tags">The editorial tags for discovery and grouping.</param>
    /// <param name="tourLinks">The tour gallery placements.</param>
    /// <param name="accessibilityTexts">Optional localized accessibility text review states.</param>
    internal PublicMediaImage(
        PublicMediaImageMetadata metadata,
        IReadOnlyList<MediaImageResponsiveVariant> responsiveVariants,
        IReadOnlyList<string> tags,
        IReadOnlyList<MediaImageTourLink> tourLinks,
        IReadOnlyList<PublicMediaImageAccessibilityText>? accessibilityTexts = null)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(responsiveVariants);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(tourLinks);

        Id = metadata.Id;
        SourceObjectKey = metadata.SourceObjectKey;
        Checksum = metadata.Checksum;
        ContentType = metadata.ContentType;
        FileSizeBytes = metadata.FileSizeBytes;
        Dimensions = metadata.Dimensions;
        ProcessingStatus = metadata.ProcessingStatus;
        _responsiveVariants = [.. responsiveVariants.Select((variant, index) => variant with { SortOrder = index })];
        _tags.AddRange(tags);
        _tourLinks = [.. tourLinks];
        AltText = metadata.AltText;
        Caption = metadata.Caption;
        IsDecorative = metadata.IsDecorative;
        RequiresHumanReview = metadata.RequiresHumanReview;
        IsAiGenerated = metadata.IsAiGenerated;
        Attribution = metadata.Attribution;
        Copyright = metadata.Copyright;
        _accessibilityTexts.AddRange(accessibilityTexts is { Count: > 0 }
            ? accessibilityTexts
            : CreateDefaultAccessibilityText(metadata));
    }

    private PublicMediaImage()
    {
        SourceObjectKey = string.Empty;
        Checksum = string.Empty;
        ContentType = string.Empty;
        Dimensions = null!;
        AltText = string.Empty;
    }

    /// <summary>
    /// Gets the stable media image identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the original source object key.
    /// </summary>
    public string SourceObjectKey { get; private set; }

    /// <summary>
    /// Gets the content checksum supplied by the media source.
    /// </summary>
    public string Checksum { get; private set; }

    /// <summary>
    /// Gets the source media content type.
    /// </summary>
    public string ContentType { get; private set; }

    /// <summary>
    /// Gets the source file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>
    /// Gets the source image dimensions.
    /// </summary>
    public MediaImageDimensions Dimensions { get; private set; }

    /// <summary>
    /// Gets the external processing state.
    /// </summary>
    public MediaImageProcessingStatus ProcessingStatus { get; private set; }

    /// <summary>
    /// Gets the public responsive renditions.
    /// </summary>
    public IReadOnlyList<MediaImageResponsiveVariant> ResponsiveVariants => _responsiveVariants.AsReadOnly();

    /// <summary>
    /// Gets the editorial tags for discovery and grouping.
    /// </summary>
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Gets the tour gallery placements.
    /// </summary>
    public IReadOnlyList<MediaImageTourLink> TourLinks => _tourLinks.AsReadOnly();

    /// <summary>
    /// Gets the accessible image description.
    /// </summary>
    public string AltText { get; private set; }

    /// <summary>
    /// Gets the optional public caption.
    /// </summary>
    public string? Caption { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the image is intentionally decorative.
    /// </summary>
    public bool IsDecorative { get; private set; }

    /// <summary>
    /// Gets a value indicating whether accessibility text needs human review before publication.
    /// </summary>
    public bool RequiresHumanReview { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current accessibility text is an AI-assisted draft.
    /// </summary>
    public bool IsAiGenerated { get; private set; }

    /// <summary>
    /// Gets localized accessibility text for this image.
    /// </summary>
    public IReadOnlyList<PublicMediaImageAccessibilityText> AccessibilityTexts => _accessibilityTexts.AsReadOnly();

    /// <summary>
    /// Gets the optional attribution text.
    /// </summary>
    public string? Attribution { get; private set; }

    /// <summary>
    /// Gets the optional copyright notice.
    /// </summary>
    public string? Copyright { get; private set; }

    /// <summary>
    /// Gets the image display order within its current tour gallery view.
    /// </summary>
    public int DisplayOrder => _tourLinks.Count == 0 ? 0 : _tourLinks.Min(link => link.DisplayOrder);

    /// <summary>
    /// Gets a value indicating whether the image is a cover image in its current tour gallery view.
    /// </summary>
    public bool IsCover => _tourLinks.Any(link => link.IsCover);

    /// <summary>
    /// Gets a value indicating whether the image has public variants that can be shown in the catalog.
    /// </summary>
    public bool HasPublicVariants => ProcessingStatus == MediaImageProcessingStatus.Ready && _responsiveVariants.Count > 0 && HasReviewedAccessibilityText;

    /// <summary>
    /// Gets a value indicating whether this image has reviewed public accessibility text.
    /// </summary>
    public bool HasReviewedAccessibilityText => !RequiresHumanReview && (IsDecorative || !string.IsNullOrWhiteSpace(AltText));

    /// <summary>
    /// Creates a public media image after validating catalog media rules.
    /// </summary>
    /// <param name="metadata">The scalar media image metadata.</param>
    /// <param name="responsiveVariants">The public responsive renditions.</param>
    /// <param name="tags">The editorial tags for discovery and grouping.</param>
    /// <param name="tourLinks">The tour gallery placements.</param>
    /// <returns>A result containing the media image when valid.</returns>
    public static Result<PublicMediaImage> Create(
        PublicMediaImageMetadata metadata,
        IReadOnlyList<MediaImageResponsiveVariant> responsiveVariants,
        IReadOnlyList<string> tags,
        IReadOnlyList<MediaImageTourLink> tourLinks)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(responsiveVariants);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(tourLinks);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (IsInvalidObjectKey(metadata.SourceObjectKey))
        {
            errors[nameof(PublicMediaImageMetadata.SourceObjectKey)] = ["Source object key must be a relative path without empty or dot segments."];
        }

        ValidateAccessibilityText(errors, metadata);
        ValidateRequiredText(errors, nameof(PublicMediaImageMetadata.Checksum), metadata.Checksum, ContractConstants.MaxChecksumLength, "Checksum is required.", nameof(PublicMediaImageMetadata.Checksum));
        ValidateRequiredText(errors, nameof(PublicMediaImageMetadata.ContentType), metadata.ContentType, ContractConstants.MaxContentTypeLength, "ContentType is required.", nameof(PublicMediaImageMetadata.ContentType));
        ValidateOptionalText(errors, nameof(PublicMediaImageMetadata.Caption), metadata.Caption, ContractConstants.MaxCaptionLength, nameof(PublicMediaImageMetadata.Caption));
        ValidateOptionalText(errors, nameof(PublicMediaImageMetadata.Attribution), metadata.Attribution, ContractConstants.MaxAttributionLength, nameof(PublicMediaImageMetadata.Attribution));
        ValidateOptionalText(errors, nameof(PublicMediaImageMetadata.Copyright), metadata.Copyright, ContractConstants.MaxCopyrightLength, nameof(PublicMediaImageMetadata.Copyright));

        if (metadata.FileSizeBytes <= 0)
        {
            errors[nameof(PublicMediaImageMetadata.FileSizeBytes)] = ["File size must be positive."];
        }

        if (metadata.Dimensions.Width <= 0 || metadata.Dimensions.Height <= 0)
        {
            errors[nameof(PublicMediaImageMetadata.Dimensions)] = ["Dimensions must be positive."];
        }

        if (metadata.ProcessingStatus == MediaImageProcessingStatus.None || !Enum.IsDefined(metadata.ProcessingStatus))
        {
            errors[nameof(PublicMediaImageMetadata.ProcessingStatus)] = ["Processing status is required."];
        }

        ValidateTourLinks(errors, tourLinks);
        ValidateResponsiveVariants(errors, responsiveVariants, metadata.ProcessingStatus);

        if (tags.Any(tag => string.IsNullOrWhiteSpace(StringSanitizer.Sanitize(tag))))
        {
            errors[nameof(Tags)] = ["Tags cannot contain blank values."];
        }

        return errors.Count > 0
            ? Result.Invalid<PublicMediaImage>("Public media image is invalid.", errors)
            : Result.Ok(new PublicMediaImage(SanitizeMetadata(metadata), SanitizeResponsiveVariants(responsiveVariants), StringSanitizer.SanitizeCollection(tags), tourLinks));
    }

    /// <summary>
    /// Orders images by catalog gallery placement.
    /// </summary>
    /// <param name="images">The images to order.</param>
    /// <returns>The ordered images.</returns>
    public static IOrderedEnumerable<PublicMediaImage> OrderForGallery(IEnumerable<PublicMediaImage> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        return images
            .OrderByDescending(image => image.IsCover)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id);
    }

    /// <summary>
    /// Returns whether this image belongs to a Catalog tour.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <returns><see langword="true" /> when this image is linked to the tour.</returns>
    public bool BelongsToTour(Guid catalogTourId)
    {
        return _tourLinks.Any(link => link.CatalogTourId == catalogTourId);
    }

    /// <summary>
    /// Returns whether this image is the cover image for a Catalog tour.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <returns><see langword="true" /> when the tour link marks this image as the cover.</returns>
    public bool IsCoverForTour(Guid catalogTourId)
    {
        return TryGetTourLink(catalogTourId, out var link) && link.IsCover;
    }

    /// <summary>
    /// Returns the display order for a Catalog tour.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <returns>The image display order for the tour gallery.</returns>
    public int GetDisplayOrderForTour(Guid catalogTourId)
    {
        return TryGetTourLink(catalogTourId, out var link) ? link.DisplayOrder : int.MaxValue;
    }

    /// <summary>
    /// Attempts to get this image's placement for a Catalog tour.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="link">The tour placement when linked.</param>
    /// <returns><see langword="true" /> when a placement exists for the requested tour.</returns>
    public bool TryGetTourLink(Guid catalogTourId, [NotNullWhen(true)] out MediaImageTourLink? link)
    {
        link = _tourLinks.FirstOrDefault(tourLink => tourLink.CatalogTourId == catalogTourId);

        return link is not null;
    }

    /// <summary>
    /// Gets a value indicating whether existing public variants should remain visible after processing fails.
    /// </summary>
    public bool CanRetainPublicVariantsAfterProcessingFailure => HasPublicVariants;

    /// <summary>
    /// Returns whether the image already has public variants for a processing version.
    /// </summary>
    /// <param name="processingVersion">The deterministic processing output version.</param>
    /// <returns><see langword="true" /> when all public variants belong to the requested version.</returns>
    public bool HasPublicVariantsForProcessingVersion(int processingVersion)
    {
        var versionSegment = string.Create(CultureInfo.InvariantCulture, $"/v{processingVersion}/");

        return HasPublicVariants
            && _responsiveVariants.All(variant => variant.ObjectKey.Contains(versionSegment, StringComparison.Ordinal));
    }

    /// <summary>
    /// Returns whether the original image should be processed for the requested deterministic output version.
    /// </summary>
    /// <param name="processingVersion">The deterministic processing output version.</param>
    /// <returns><see langword="true" /> when processing is needed for the requested version.</returns>
    public bool ShouldProcessOriginal(int processingVersion)
    {
        return !HasPublicVariantsForProcessingVersion(processingVersion);
    }

    /// <summary>
    /// Creates a copy with updated processing output.
    /// </summary>
    /// <param name="dimensions">The processed image dimensions.</param>
    /// <param name="status">The processing status.</param>
    /// <param name="variants">The processed public variants.</param>
    /// <returns>A result containing the updated media image when valid.</returns>
    public Result<PublicMediaImage> WithProcessingResult(
        MediaImageDimensions dimensions,
        MediaImageProcessingStatus status,
        IReadOnlyList<MediaImageResponsiveVariant> variants)
    {
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(variants);

        return Create(
            new PublicMediaImageMetadata
            {
                Id = Id,
                SourceObjectKey = SourceObjectKey,
                Checksum = Checksum,
                ContentType = ContentType,
                FileSizeBytes = FileSizeBytes,
                Dimensions = dimensions,
                ProcessingStatus = status,
                AltText = AltText,
                Caption = Caption,
                IsDecorative = IsDecorative,
                RequiresHumanReview = RequiresHumanReview,
                IsAiGenerated = IsAiGenerated,
                Attribution = Attribution,
                Copyright = Copyright,
            },
            variants,
            Tags,
            TourLinks);
    }

    /// <summary>
    /// Adds or replaces AI-assisted draft accessibility text for a language.
    /// </summary>
    /// <param name="language">The text language.</param>
    /// <param name="altText">The drafted accessible image description.</param>
    /// <param name="caption">The optional drafted caption.</param>
    /// <returns>A result indicating whether the draft was accepted.</returns>
    public Result SetAiDraftAccessibilityText(PublicContentLanguage language, string altText, string? caption)
    {
        var result = PublicMediaImageAccessibilityText.CreateAiDraft(language, altText, caption);
        if (result.IsFailure)
        {
            return Result.Invalid(result.ErrorDetails.Detail, ToValidationDictionary(result.ErrorDetails.ValidationErrors));
        }

        ReplaceAccessibilityText(result.Value);
        if (language == PublicContentLanguage.EnUs)
        {
            AltText = result.Value.AltText ?? string.Empty;
            Caption = result.Value.Caption;
            IsDecorative = result.Value.IsDecorative;
            RequiresHumanReview = true;
            IsAiGenerated = true;
        }

        return Result.Ok();
    }

    /// <summary>
    /// Adds or replaces editor-reviewed accessibility text for a language.
    /// </summary>
    /// <param name="language">The text language.</param>
    /// <param name="altText">The approved accessible image description.</param>
    /// <param name="caption">The optional approved caption.</param>
    /// <param name="isDecorative">Whether the image is intentionally decorative.</param>
    /// <returns>A result indicating whether the reviewed text was accepted.</returns>
    public Result SetReviewedAccessibilityText(PublicContentLanguage language, string? altText, string? caption, bool isDecorative)
    {
        var result = PublicMediaImageAccessibilityText.CreateReviewed(language, altText, caption, isDecorative);
        if (result.IsFailure)
        {
            return Result.Invalid(result.ErrorDetails.Detail, ToValidationDictionary(result.ErrorDetails.ValidationErrors));
        }

        ReplaceAccessibilityText(result.Value);
        if (language == PublicContentLanguage.EnUs)
        {
            AltText = result.Value.AltText ?? string.Empty;
            Caption = result.Value.Caption;
            IsDecorative = result.Value.IsDecorative;
            RequiresHumanReview = false;
            IsAiGenerated = false;
        }

        return Result.Ok();
    }

    private static PublicMediaImageMetadata SanitizeMetadata(PublicMediaImageMetadata metadata)
    {
        return new PublicMediaImageMetadata
        {
            Id = metadata.Id,
            SourceObjectKey = StringSanitizer.Sanitize(metadata.SourceObjectKey) ?? string.Empty,
            Checksum = StringSanitizer.Sanitize(metadata.Checksum),
            ContentType = StringSanitizer.Sanitize(metadata.ContentType),
            FileSizeBytes = metadata.FileSizeBytes,
            Dimensions = metadata.Dimensions,
            ProcessingStatus = metadata.ProcessingStatus,
            AltText = StringSanitizer.Sanitize(metadata.AltText),
            Caption = StringSanitizer.Sanitize(metadata.Caption),
            IsDecorative = metadata.IsDecorative,
            RequiresHumanReview = metadata.RequiresHumanReview,
            IsAiGenerated = metadata.IsAiGenerated,
            Attribution = StringSanitizer.Sanitize(metadata.Attribution),
            Copyright = StringSanitizer.Sanitize(metadata.Copyright),
        };
    }

    private static MediaImageResponsiveVariant[] SanitizeResponsiveVariants(IEnumerable<MediaImageResponsiveVariant> variants)
    {
        return [.. variants.Select(static variant => variant with
        {
            ObjectKey = StringSanitizer.Sanitize(variant.ObjectKey) ?? string.Empty,
            ContentType = StringSanitizer.Sanitize(variant.ContentType)
        })];
    }

    private static void ValidateTourLinks(Dictionary<string, string[]> errors, IReadOnlyCollection<MediaImageTourLink> tourLinks)
    {
        if (tourLinks.Count == 0)
        {
            errors[nameof(TourLinks)] = ["At least one tour link is required."];
        }
        else if (tourLinks.Any(link => link.CatalogTourId == Guid.Empty || link.DisplayOrder < 0))
        {
            errors[nameof(TourLinks)] = ["Tour links require a tour id and non-negative display order."];
        }
        else if (tourLinks.Select(link => link.CatalogTourId).Distinct().Count() != tourLinks.Count)
        {
            errors[nameof(TourLinks)] = ["Tour links cannot contain duplicate tour ids."];
        }
    }

    private void ReplaceAccessibilityText(PublicMediaImageAccessibilityText text)
    {
        _accessibilityTexts.RemoveAll(item => item.Language == text.Language);
        _accessibilityTexts.Add(text);
    }

    private static IReadOnlyList<PublicMediaImageAccessibilityText> CreateDefaultAccessibilityText(PublicMediaImageMetadata metadata)
    {
        var result = PublicMediaImageAccessibilityText.Create(
            PublicContentLanguage.EnUs,
            metadata.AltText,
            metadata.Caption,
            metadata.IsDecorative,
            metadata.RequiresHumanReview,
            metadata.IsAiGenerated);

        return result.IsSuccess ? [result.Value] : [];
    }

    private static void ValidateAccessibilityText(Dictionary<string, string[]> errors, PublicMediaImageMetadata metadata)
    {
        var result = PublicMediaImageAccessibilityText.Create(
            PublicContentLanguage.EnUs,
            metadata.AltText,
            metadata.Caption,
            metadata.IsDecorative,
            metadata.RequiresHumanReview,
            metadata.IsAiGenerated);

        if (result.IsFailure && result.ErrorDetails?.ValidationErrors is not null)
        {
            foreach (var error in result.ErrorDetails.ValidationErrors)
            {
                errors[error.Key] = error.Value.ToArray();
            }
        }
    }

    private static Dictionary<string, string[]> ToValidationDictionary(IReadOnlyDictionary<string, IReadOnlyList<string>>? validationErrors)
    {
        return validationErrors?.ToDictionary(error => error.Key, error => error.Value.ToArray(), StringComparer.Ordinal)
            ?? new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(AccessibilityTexts)] = ["Accessibility text is invalid."]
            };
    }

    private static void ValidateResponsiveVariants(
        Dictionary<string, string[]> errors,
        IReadOnlyCollection<MediaImageResponsiveVariant> responsiveVariants,
        MediaImageProcessingStatus processingStatus)
    {
        if (responsiveVariants.Any(IsInvalidResponsiveVariant))
        {
            errors[nameof(ResponsiveVariants)] = ["Responsive variants must include valid relative object keys, positive dimensions, content type, and file size."];
        }
        else if (processingStatus == MediaImageProcessingStatus.Ready && responsiveVariants.Count == 0)
        {
            errors[nameof(ResponsiveVariants)] = ["Ready images require at least one processed public variant."];
        }
    }

    private static bool IsInvalidResponsiveVariant(MediaImageResponsiveVariant variant)
    {
        var contentType = StringSanitizer.Sanitize(variant.ContentType);

        return IsInvalidObjectKey(variant.ObjectKey)
            || variant.Width <= 0
            || variant.Height <= 0
            || string.IsNullOrWhiteSpace(contentType)
            || contentType.Length > ContractConstants.MaxContentTypeLength
            || variant.FileSizeBytes <= 0;
    }

    private static bool IsInvalidObjectKey(string? objectKey)
    {
        var sanitized = StringSanitizer.Sanitize(objectKey);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return true;
        }

        if (sanitized.StartsWith('/') || sanitized.StartsWith('\\') || IsWindowsRootedPath(sanitized))
        {
            return true;
        }

        return sanitized.Replace('\\', '/').Split('/').Any(static segment =>
            segment.Length == 0 || segment is "." or "..");
    }

    private static bool IsWindowsRootedPath(string objectKey) =>
        objectKey.Length >= 2 && char.IsAsciiLetter(objectKey[0]) && objectKey[1] == ':';

    private static void ValidateRequiredText(
        Dictionary<string, string[]> errors,
        string field,
        string value,
        int maxLength,
        string requiredMessage,
        string displayName)
    {
        var sanitized = StringSanitizer.Sanitize(value);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            errors[field] = [requiredMessage];
        }
        else if (sanitized.Length > maxLength)
        {
            errors[field] = [$"{displayName} cannot exceed {maxLength} characters."];
        }
    }

    private static void ValidateOptionalText(
        Dictionary<string, string[]> errors,
        string field,
        string? value,
        int maxLength,
        string displayName)
    {
        var sanitized = StringSanitizer.Sanitize(value);
        if (sanitized?.Length > maxLength)
        {
            errors[field] = [$"{displayName} cannot exceed {maxLength} characters."];
        }
    }
}
