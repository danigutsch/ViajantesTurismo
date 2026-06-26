using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public static class CatalogProjectionRunnerTestsHelpers
{
    public static EventEnvelope CreateEnvelope(long position, CatalogTourDraftCreated draftCreated, DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(draftCreated);

        return new EventEnvelope(
            CatalogTourStreamIds.FromAdminTourId(draftCreated.AdminTourId),
            position,
            StreamRevision.From(1),
            Guid.CreateVersion7(),
            nameof(CatalogTourDraftCreated),
            draftCreated,
            recordedAt);
    }
}
