using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.Idempotency;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Binds completed duplicate observations to the Catalog inbox and consumer.</summary>
internal sealed class CatalogInboxDuplicateRecorder(IntegrationEventInboxDuplicateRecorder<CatalogDbContext> recorder)
    : ICatalogInboxDuplicateRecorder
{
    /// <summary>Registers the Catalog inbox mapping and its configured duplicate recorder.</summary>
    internal static void Register(IServiceCollection services)
    {
        services.AddPostgreSqlIntegrationEventInboxDuplicates<CatalogDbContext>(
            IntegrationEventConsumerNames.Catalog, ICatalogInboxDuplicateRecorder.ScopePrefix);
        services.TryAddScoped<ICatalogInboxDuplicateRecorder, CatalogInboxDuplicateRecorder>();
    }

    /// <inheritdoc />
    public Task Record(IdempotencyScope scope, Guid eventId, DateTimeOffset observedAt, CancellationToken ct) =>
        recorder.Record(scope, eventId, observedAt, ct);
}
