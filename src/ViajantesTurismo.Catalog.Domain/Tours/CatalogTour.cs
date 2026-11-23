using PandaBear.BuildingBlocks;
using PandaBear.Galleries;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using PandaBear.EventSourcing;
using PandaBear.InputNormalization;
using PandaBear.Results;

namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Customer-facing tour presentation aggregate owned by the Catalog context.
/// </summary>
public sealed class CatalogTour : EventSourcedAggregateRoot<Guid>
{
    private Guid id;
    private long? legacyBootstrapThroughSequence;

    private static readonly AlbumRules GalleryRules = new(PublicContentCultures.Supported, CatalogDomainLimits.MaxGalleryPhotos)
    {
        MaxTitleLength = CatalogDomainLimits.MaxNameLength,
        MaxSlugLength = CatalogDomainLimits.MaxSlugLength
    };

    private CatalogTour()
    {
    }

    /// <inheritdoc />
    public override Guid Id => id;

    /// <summary>
    /// Gets the source Admin tour identifier.
    /// </summary>
    public Guid AdminTourId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the source Admin tour business identifier.
    /// </summary>
    public string Identifier { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the draft presentation title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the stable public URL slug.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the concise customer-facing summary.
    /// </summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the detailed customer-facing description.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the plain-text customer-facing itinerary.
    /// </summary>
    public string Itinerary { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional search-engine title override.
    /// </summary>
    public string SeoTitle { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional search-engine description override.
    /// </summary>
    public string SeoDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the tour is visible on the public website.
    /// </summary>
    public bool IsPublished
    {
        get; private set;
    }

    /// <summary>Gets the retirement that permanently withdrew this tour, when present.</summary>
    public Guid? RetirementId
    {
        get; private set;
    }

    /// <summary>Gets the authored photo order, or null for a tour whose placement still comes from legacy media links.</summary>
    public IReadOnlyList<Guid>? GalleryPhotoIds
    {
        get; private set;
    }

    /// <summary>Gets the cover chosen by the editor. The first eligible photo is the fallback.</summary>
    public Guid? GalleryCoverId
    {
        get; private set;
    }

    /// <summary>Gets the latest placement sequence resolved in the event stream.</summary>
    public long GalleryIntentSequence
    {
        get;
        private set;
    }

    /// <summary>Determines whether gallery content can change with the caller's publication authority.</summary>
    /// <param name="canPublish">Whether the caller can approve public Catalog content.</param>
    /// <param name="isPubliclyVisible">Whether the current public projection still exposes the tour.</param>
    /// <param name="projectionVersion">The event stream version represented by the public projection.</param>
    /// <returns>Whether gallery changes are permitted in the current publication state.</returns>
    public bool CanEditGallery(bool canPublish, bool isPubliclyVisible, long projectionVersion)
    {
        if (RetirementId is not null)
        {
            return false;
        }

        return canPublish || (!IsPublished && !isPubliclyVisible && projectionVersion == Version);
    }

    /// <summary>
    /// Creates a draft Catalog tour from an Admin tour-created event.
    /// </summary>
    public static CatalogTour CreateDraft(Guid adminTourId, string identifier, string title, Guid sourceEventId)
    {
        var catalogTourId = Guid.CreateVersion7();
        return CreateDraft(
            catalogTourId,
            adminTourId,
            identifier,
            title,
            sourceEventId,
            CatalogTourSlug.CreateInitial(StringSanitizer.Sanitize(identifier), catalogTourId));
    }

    /// <summary>
    /// Creates a draft Catalog tour with a preselected canonical initial slug.
    /// </summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="adminTourId">The source Admin tour identifier.</param>
    /// <param name="identifier">The source Admin tour business identifier.</param>
    /// <param name="title">The initial customer-facing title.</param>
    /// <param name="sourceEventId">The integration event that caused creation.</param>
    /// <param name="initialSlug">The selected canonical initial slug.</param>
    /// <returns>A new draft Catalog tour.</returns>
    public static CatalogTour CreateDraft(
        Guid catalogTourId,
        Guid adminTourId,
        string identifier,
        string title,
        Guid sourceEventId,
        string initialSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(initialSlug);

        if (!CatalogTourSlug.IsCanonical(initialSlug))
        {
            throw new ArgumentException("The initial Catalog tour slug must be canonical.", nameof(initialSlug));
        }

        var sanitizedIdentifier = StringSanitizer.Sanitize(identifier);
        var sanitizedTitle = StringSanitizer.Sanitize(title);

        var catalogTour = new CatalogTour();
        catalogTour.AddEvent(new CatalogTourDraftCreated(
            catalogTourId,
            adminTourId,
            sanitizedIdentifier,
            sanitizedTitle,
            sourceEventId,
            initialSlug));

        return catalogTour;
    }

    /// <summary>
    /// Rehydrates a Catalog tour from its persisted event stream.
    /// </summary>
    /// <param name="events">The persisted event payloads in stream order.</param>
    /// <returns>The rehydrated Catalog tour.</returns>
    public static CatalogTour Rehydrate(IEnumerable<object> events)
    {
        var catalogTour = new CatalogTour();
        catalogTour.Replay(events);
        return catalogTour;
    }

    /// <summary>Retires an existing tour or closes its stream before a delayed create can arrive.</summary>
    public void Retire(Guid adminTourId, Guid retirementId)
    {
        if (adminTourId == Guid.Empty || retirementId == Guid.Empty)
        {
            throw new ArgumentException("Tour retirement requires source and retirement identifiers.");
        }

        if (AdminTourId != Guid.Empty && AdminTourId != adminTourId)
        {
            throw new InvalidOperationException("Retirement must match the Admin tour identifier.");
        }

        if (RetirementId is null)
        {
            AddEvent(new CatalogTourRetired(adminTourId, retirementId));
        }
        else if (RetirementId != retirementId)
        {
            throw new InvalidOperationException("The Catalog tour already has a different retirement identifier.");
        }
    }

    /// <summary>
    /// Updates the editable customer-facing presentation values.
    /// </summary>
    /// <param name="title">The public tour title.</param>
    /// <param name="slug">The stable public URL slug.</param>
    /// <param name="summary">The concise customer-facing summary.</param>
    /// <param name="description">The detailed customer-facing description.</param>
    /// <param name="itinerary">The plain-text customer-facing itinerary.</param>
    /// <param name="seoTitle">The optional search-engine title override.</param>
    /// <param name="seoDescription">The optional search-engine description override.</param>
    /// <returns>
    /// A success result, a conflict when the tour is published, or a validation failure when values are invalid.
    /// </returns>
    public Result ChangePresentation(
        string title,
        string slug,
        string summary,
        string description,
        string itinerary,
        string seoTitle,
        string seoDescription)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(slug);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(itinerary);
        ArgumentNullException.ThrowIfNull(seoTitle);
        ArgumentNullException.ThrowIfNull(seoDescription);

        if (RetirementId is not null)
        {
            return CatalogTourErrors.Retired();
        }

        if (IsPublished)
        {
            return CatalogTourErrors.PublishedPresentationChange();
        }

        var presentation = CatalogTourPresentation.Create(
            title,
            slug,
            summary,
            description,
            itinerary,
            seoTitle,
            seoDescription);
        if (presentation.IsFailure)
        {
            return presentation.ConvertError();
        }

        var value = presentation.Value;
        if (Title == value.Title
            && Slug == value.Slug
            && Summary == value.Summary
            && Description == value.Description
            && Itinerary == value.Itinerary
            && SeoTitle == value.SeoTitle
            && SeoDescription == value.SeoDescription)
        {
            return Result.Ok();
        }

        AddEvent(new CatalogTourPresentationChanged(
            Id,
            value.Title,
            value.Slug,
            value.Summary,
            value.Description,
            value.Itinerary,
            value.SeoTitle,
            value.SeoDescription));
        return Result.Ok();
    }

    /// <summary>
    /// Makes the tour visible on the public website when its minimum public content is complete.
    /// </summary>
    /// <returns>A success result, or a validation failure when required public content is missing.</returns>
    public Result Publish()
    {
        if (RetirementId is not null)
        {
            return CatalogTourErrors.Retired();
        }

        if (IsPublished)
        {
            return Result.Ok();
        }

        if (string.IsNullOrWhiteSpace(Title)
            || string.IsNullOrWhiteSpace(Slug)
            || string.IsNullOrWhiteSpace(Summary))
        {
            return CatalogTourErrors.PublicationNotReady();
        }

        AddEvent(new CatalogTourPublished(Id));
        return Result.Ok();
    }

    /// <summary>
    /// Removes the tour from the public website.
    /// </summary>
    public void Unpublish()
    {
        if (!IsPublished)
        {
            return;
        }

        AddEvent(new CatalogTourUnpublished(Id));
    }

    /// <summary>Records the ordered photo selection and optional cover of the tour album.</summary>
    /// <param name="photoIds">The ordered selection of Catalog photos.</param>
    /// <param name="coverId">The chosen cover, or null to use the first eligible photo.</param>
    /// <param name="photos">Catalog's knowledge of the photo inventory.</param>
    /// <returns>A success result or the album validation failure.</returns>
    /// <remarks>Pending photos can be placed. Public gallery reads apply the current rendition and locale review rules.</remarks>
    public Result ChangeGallery(IReadOnlyList<Guid> photoIds, Guid? coverId, IAlbumPhotoEligibility<Guid> photos) =>
        ChangeGallery(photoIds, coverId, photos, null, null, null);

    /// <summary>Records the legacy placement once, together with the journal boundary observed under the tour lease.</summary>
    public Result BootstrapLegacyGallery(
        IReadOnlyList<Guid> photoIds, Guid? coverId, IAlbumPhotoEligibility<Guid> photos, long throughSequence)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(throughSequence);

        return GalleryPhotoIds is null
            ? ChangeGallery(photoIds, coverId, photos, null, null, throughSequence)
            : Result.Ok();
    }

    /// <summary>Determines whether replay can acknowledge an upload already represented by the baseline without changing placement.</summary>
    public bool IncludesBootstrappedUpload(Guid photoId, long sequence) =>
        RetirementId is null && sequence > 0 && sequence <= legacyBootstrapThroughSequence && GalleryPhotoIds?.Contains(photoId) == true;

    /// <summary>Records the next accepted placement intent and the resulting gallery snapshot.</summary>
    public Result ApplyGalleryIntent(
        IReadOnlyList<Guid> photoIds, Guid? coverId, IAlbumPhotoEligibility<Guid> photos, Guid intentId, long sequence)
    {
        if (sequence <= GalleryIntentSequence)
        {
            return Result.Ok();
        }

        if (sequence != GalleryIntentSequence + 1)
        {
            return Result.Conflict("The preceding gallery placement intent is still pending.");
        }

        return ChangeGallery(photoIds, coverId, photos, intentId, sequence, null);
    }

    /// <summary>Resolves a draft-only intent whose queued placement would now change a published gallery.</summary>
    public bool RejectGalleryIntentAfterPublication(bool canPublish, Guid intentId, long sequence)
    {
        if (canPublish || !IsPublished)
        {
            return false;
        }

        AddEvent(new CatalogTourGalleryIntentRejected(Id, intentId, sequence));
        return true;
    }

    /// <summary>Appends a newly accepted upload to the authored photo order and preserves the chosen cover.</summary>
    public Result ApplyGalleryUpload(Guid photoId, IAlbumPhotoEligibility<Guid> photos, Guid intentId, long sequence)
    {
        if (GalleryPhotoIds is not { } gallery)
        {
            return Result.Conflict("The gallery needs an authored baseline before applying uploaded photos.");
        }

        IReadOnlyList<Guid> placement = gallery.Contains(photoId) ? gallery : [.. gallery, photoId];
        return ApplyGalleryIntent(placement, GalleryCoverId, photos, intentId, sequence);
    }

    private Result ChangeGallery(
        IReadOnlyList<Guid> photoIds, Guid? coverId, IAlbumPhotoEligibility<Guid> photos, Guid? intentId, long? intentSequence,
        long? bootstrapThroughSequence)
    {
        ArgumentNullException.ThrowIfNull(photoIds);
        ArgumentNullException.ThrowIfNull(photos);

        if (RetirementId is not null)
        {
            return CatalogTourErrors.Retired();
        }

        var translation = new AlbumTranslationDefinition(PublicContentCultures.Supported.Default.Value, Title, Slug, PublicationStatus.Draft);
        var album = Album.Create(new AlbumDefinition<Guid, Guid>(0, Id, photoIds, coverId, [translation]), GalleryRules, photos);
        if (album.IsFailure)
        {
            return album.ConvertError();
        }

        var authored = album.Value;
        if (intentSequence is not null || GalleryPhotoIds is null
            || !GalleryPhotoIds.SequenceEqual(authored.PhotoIds) || GalleryCoverId != authored.CoverId)
        {
            AddEvent(new CatalogTourGalleryChanged(Id, authored.PhotoIds, authored.CoverId, intentId, intentSequence, bootstrapThroughSequence));
        }

        return Result.Ok();
    }

    /// <inheritdoc />
    protected override void ApplyEvent(object domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        switch (domainEvent)
        {
            case CatalogTourDraftCreated created:
                id = created.CatalogTourId;
                AdminTourId = created.AdminTourId;
                Identifier = created.Identifier;
                Title = created.Title;
                Slug = CatalogTourSlug.RequireCanonical(created.InitialSlug);
                break;
            case CatalogTourPresentationChanged presentationChanged:
                EnsureCatalogTourId(presentationChanged.CatalogTourId);
                Title = presentationChanged.Title;
                Slug = presentationChanged.Slug;
                Summary = presentationChanged.Summary;
                Description = presentationChanged.Description;
                Itinerary = presentationChanged.Itinerary;
                SeoTitle = presentationChanged.SeoTitle;
                SeoDescription = presentationChanged.SeoDescription;
                break;
            case CatalogTourGalleryChanged galleryChanged:
                EnsureCatalogTourId(galleryChanged.CatalogTourId);
                if (galleryChanged.LegacyBootstrapThroughSequence is { } bootstrapWatermark)
                {
                    if (GalleryPhotoIds is not null)
                    {
                        throw new InvalidOperationException("Catalog can bootstrap only the first authored gallery snapshot.");
                    }

                    legacyBootstrapThroughSequence = bootstrapWatermark;
                }

                if (galleryChanged.IntentSequence is { } sequence)
                {
                    AdvanceGalleryIntentSequence(sequence);
                }

                GalleryPhotoIds = galleryChanged.PhotoIds;
                GalleryCoverId = galleryChanged.CoverId;
                break;
            case CatalogTourGalleryIntentRejected rejected:
                EnsureCatalogTourId(rejected.CatalogTourId);
                AdvanceGalleryIntentSequence(rejected.IntentSequence);
                break;
            case CatalogTourPublished published:
                EnsureCatalogTourId(published.CatalogTourId);
                IsPublished = true;
                break;
            case CatalogTourUnpublished unpublished:
                EnsureCatalogTourId(unpublished.CatalogTourId);
                IsPublished = false;
                break;
            case CatalogTourRetired retired:
                AdminTourId = retired.AdminTourId;
                RetirementId = retired.RetirementId;
                IsPublished = false;
                break;
            default:
                throw new InvalidOperationException(
                    $"Catalog tour cannot apply event type '{domainEvent.GetType().FullName}'.");
        }
    }

    private void AdvanceGalleryIntentSequence(long sequence)
    {
        if (sequence != GalleryIntentSequence + 1)
        {
            throw new InvalidOperationException("Catalog gallery intents must replay in order.");
        }

        GalleryIntentSequence = sequence;
    }

    private void EnsureCatalogTourId(Guid catalogTourId)
    {
        if (catalogTourId != Id)
        {
            throw new InvalidOperationException("Catalog tour events must match the aggregate identifier.");
        }
    }
}
