using PandaBear.EventSourcing;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Projects Catalog tour events into management/public tour read model rows.
/// </summary>
public sealed class CatalogTourReadModelProjection(
    ICatalogTourReadModelStore readModelStore,
    ICatalogGalleryPlacementIntentStore? placementIntents = null) : IProjection
{
    /// <inheritdoc />
    public string Name => "catalog.tours.read-model";

    /// <inheritdoc />
    public async ValueTask Apply(EventEnvelope envelope, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        switch (envelope.Data)
        {
            case CatalogTourDraftCreated draftCreated:
                await readModelStore.UpsertDraft(
                    CatalogTourDraftReadModel.FromDraftCreated(draftCreated, envelope.Position, envelope.RecordedAt),
                    ct);
                return;
            case CatalogTourPresentationChanged presentationChanged:
                await readModelStore.UpdatePresentation(
                    presentationChanged.CatalogTourId,
                    new CatalogTourPresentationUpdate(
                        presentationChanged.Title,
                        presentationChanged.Slug,
                        presentationChanged.Summary,
                        presentationChanged.Description,
                        presentationChanged.Itinerary,
                        presentationChanged.SeoTitle,
                        presentationChanged.SeoDescription),
                    envelope.Revision.Value,
                    envelope.Position,
                    envelope.RecordedAt,
                    ct);
                return;
            case CatalogTourGalleryChanged gallery:
                if (await readModelStore.UpdateGallery(gallery, envelope.Revision.Value, envelope.Position, envelope.RecordedAt, ct)
                    is null)
                {
                    throw new InvalidOperationException("The Catalog tour draft must be projected before its gallery.");
                }

                if (gallery.LegacyBootstrapThroughSequence is { } throughSequence && throughSequence > 0)
                {
                    var journal = placementIntents ?? throw new InvalidOperationException("The Catalog gallery journal is missing.");
                    await journal.ResumeBootstrap(gallery.CatalogTourId, throughSequence, envelope.EventId, ct).ConfigureAwait(false);
                }

                return;
            case CatalogTourGalleryIntentRejected rejected:
                if (!await readModelStore.AdvanceStreamVersion(rejected.CatalogTourId, envelope.Revision.Value, ct).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("The Catalog tour draft must be projected before its placement outcome.");
                }

                return;
            case CatalogTourPublished published:
                await readModelStore.SetPublicationStatus(
                    published.CatalogTourId,
                    isPublished: true,
                    envelope.Revision.Value,
                    envelope.Position,
                    envelope.RecordedAt,
                    ct);
                return;
            case CatalogTourUnpublished unpublished:
                await readModelStore.SetPublicationStatus(
                    unpublished.CatalogTourId,
                    isPublished: false,
                    envelope.Revision.Value,
                    envelope.Position,
                    envelope.RecordedAt,
                    ct);
                return;
            default:
                return;
        }
    }
}
