using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Projects Catalog tour events into management/public tour read model rows.
/// </summary>
public sealed class CatalogTourReadModelProjection(
    ICatalogTourReadModelStore readModelStore) : IProjection
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
