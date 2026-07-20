using SharedKernel.EventSourcing;
using SharedKernel.InputNormalization;

namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Customer-facing tour presentation aggregate owned by the Catalog context.
/// </summary>
public sealed class CatalogTour : EventSourcedAggregateRoot<Guid>
{
    private Guid id;

    private CatalogTour()
    {
    }

    /// <inheritdoc />
    public override Guid Id => id;

    /// <summary>
    /// Gets the source Admin tour identifier.
    /// </summary>
    public Guid AdminTourId { get; private set; }

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
    public bool IsPublished { get; private set; }

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
    public void ChangePresentation(
        string title,
        string slug,
        string summary,
        string description,
        string itinerary,
        string seoTitle,
        string seoDescription)
    {
        if (IsPublished)
        {
            throw new CatalogTourPublishedPresentationChangeException();
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(itinerary);
        ArgumentNullException.ThrowIfNull(seoTitle);
        ArgumentNullException.ThrowIfNull(seoDescription);

        if (!CatalogTourSlug.IsCanonical(slug))
        {
            throw new ArgumentException("Catalog tour slugs must be canonical lowercase ASCII path segments.", nameof(slug));
        }

        if (Title == title
            && Slug == slug
            && Summary == summary
            && Description == description
            && Itinerary == itinerary
            && SeoTitle == seoTitle
            && SeoDescription == seoDescription)
        {
            return;
        }

        AddEvent(new CatalogTourPresentationChanged(
            Id,
            title,
            slug,
            summary,
            description,
            itinerary,
            seoTitle,
            seoDescription));
    }

    /// <summary>
    /// Makes the tour visible on the public website when its minimum public content is complete.
    /// </summary>
    public void Publish()
    {
        if (IsPublished)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Title)
            || string.IsNullOrWhiteSpace(Slug)
            || string.IsNullOrWhiteSpace(Summary))
        {
            throw new CatalogTourPublicationNotReadyException();
        }

        AddEvent(new CatalogTourPublished(Id));
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
            case CatalogTourPublished published:
                EnsureCatalogTourId(published.CatalogTourId);
                IsPublished = true;
                break;
            case CatalogTourUnpublished unpublished:
                EnsureCatalogTourId(unpublished.CatalogTourId);
                IsPublished = false;
                break;
            default:
                throw new InvalidOperationException(
                    $"Catalog tour cannot apply event type '{domainEvent.GetType().FullName}'.");
        }
    }

    private void EnsureCatalogTourId(Guid catalogTourId)
    {
        if (catalogTourId != Id)
        {
            throw new InvalidOperationException("Catalog tour events must match the aggregate identifier.");
        }
    }
}
