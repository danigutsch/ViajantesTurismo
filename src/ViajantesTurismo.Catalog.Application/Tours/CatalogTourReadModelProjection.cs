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

        if (envelope.Data is not CatalogTourDraftCreated draftCreated)
        {
            return;
        }

        await readModelStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                draftCreated.CatalogTourId,
                draftCreated.AdminTourId,
                draftCreated.Identifier,
                draftCreated.Title,
                envelope.Position,
                envelope.RecordedAt),
            ct);
    }
}
