using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.System.Operations;

/// <summary>Provides the visible evidence expected after delivering owned synthetic events.</summary>
/// <param name="ManagementUrl">The Management base address for the isolated stack.</param>
/// <param name="DuplicateEventId">The event delivered once and then skipped twice.</param>
/// <param name="SingleDeliveryEventId">The event delivered only once.</param>
/// <param name="LastObservedAt">The expected timestamp of the second completed duplicate.</param>
internal sealed record CatalogInboxScreenScenario(
    Uri ManagementUrl,
    Guid DuplicateEventId,
    Guid SingleDeliveryEventId,
    DateTimeOffset LastObservedAt)
{
    /// <summary>Starts the independent stack and delivers both events through the real Catalog inbox decorator.</summary>
    internal static async Task<CatalogInboxScreenScenario> Create(AspireSystemTestFixture fixture, CancellationToken ct)
    {
        var app = await fixture.GetCatalogInboxBrowserApplication(ct);
        var managementUrl = app.GetEndpoint(ResourceNames.WebApp, "https");
        var catalogConnection = await app.GetConnectionString(ResourceNames.CatalogDatabase, ct);
        await using var delivery = await CatalogGalleryIntentPostgreSqlScenario.Create(catalogConnection, ct);

        var duplicate = new AdminTourCreatedIntegrationEvent(Guid.CreateVersion7(), TimeProvider.System.GetUtcNow(),
            Guid.CreateVersion7(), "screen-atlas", "Atlas");
        var single = new AdminTourCreatedIntegrationEvent(Guid.CreateVersion7(), TimeProvider.System.GetUtcNow(),
            Guid.CreateVersion7(), "screen-alps", "Alps");
        await delivery.Deliver(duplicate, ct);
        await delivery.Deliver(single, ct);
        delivery.AdvanceBeyondInboxLease();
        await delivery.Deliver(duplicate, ct);
        var lastObservedAt = delivery.AdvanceBeyondInboxLease();
        await delivery.Deliver(duplicate, ct);

        return new CatalogInboxScreenScenario(managementUrl, duplicate.EventId, single.EventId, lastObservedAt);
    }
}
