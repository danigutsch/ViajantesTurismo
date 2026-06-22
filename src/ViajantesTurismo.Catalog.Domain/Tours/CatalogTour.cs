using SharedKernel.EventSourcing;

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
    /// Creates a draft Catalog tour from an Admin tour-created event.
    /// </summary>
    public static CatalogTour CreateDraft(Guid adminTourId, string identifier, string title, Guid sourceEventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var catalogTour = new CatalogTour();
        catalogTour.AddEvent(new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            adminTourId,
            identifier.Trim(),
            title.Trim(),
            sourceEventId));

        return catalogTour;
    }

    /// <inheritdoc />
    protected override void ApplyEvent(object domainEvent)
    {
        if (domainEvent is CatalogTourDraftCreated created)
        {
            id = created.CatalogTourId;
            AdminTourId = created.AdminTourId;
            Identifier = created.Identifier;
            Title = created.Title;
        }
    }
}
