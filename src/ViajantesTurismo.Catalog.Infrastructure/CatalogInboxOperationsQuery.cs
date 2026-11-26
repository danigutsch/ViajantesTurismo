using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Projects the Catalog-owned SharedKernel inbox evidence to read-only Operations contracts.</summary>
internal sealed class CatalogInboxOperationsQuery(CatalogDbContext dbContext) : ICatalogInboxOperationsQuery
{
    /// <inheritdoc />
    public async Task<GetCatalogInboxHealthDto> ReadHealth(CancellationToken ct)
    {
        const string consumerName = IntegrationEventConsumerNames.Catalog;
        var health = await IntegrationEventInboxDuplicates.ReadHealth(dbContext, consumerName, ct)
            .ConfigureAwait(false);
        return new GetCatalogInboxHealthDto(health.EntriesWithDuplicates, health.DuplicateCount, health.LastObservedAt)
        {
            ConsumerName = consumerName
        };
    }

    /// <inheritdoc />
    public async Task<GetCatalogInboxDuplicatePageDto> ReadPage(CatalogInboxDuplicateFilters filters, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filters);

        IntegrationEventInboxDuplicateCursor? after = filters.AfterObservedAt is { } time && filters.AfterEventId is { } id
            ? new IntegrationEventInboxDuplicateCursor(time, id)
            : null;
        var page = await IntegrationEventInboxDuplicates.ReadPage(
            dbContext, IntegrationEventConsumerNames.Catalog, filters.EventId, filters.ObservedSince, filters.Limit, after, ct)
            .ConfigureAwait(false);
        return new GetCatalogInboxDuplicatePageDto(
            [.. page.Rows.Select(static row => new CatalogInboxDuplicateRowDto(row.EventId, row.DuplicateCount, row.LastObservedAt))],
            page.Next?.LastObservedAt, page.Next?.EventId);
    }
}
