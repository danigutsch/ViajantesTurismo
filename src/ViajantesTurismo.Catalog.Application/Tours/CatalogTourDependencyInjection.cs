using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SharedKernel.EventSourcing;
using SharedKernel.Idempotency;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>Registers tour application operations and Admin tour event consumption.</summary>
internal static class CatalogTourDependencyInjection
{
    /// <summary>Registers the Catalog tour feature.</summary>
    internal static void AddCatalogToursApplication(this IServiceCollection services)
    {
        services.TryAddScoped<IIntegrationEventHandler<AdminTourCreatedIntegrationEvent>>(sp =>
            new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
                new AdminTourCreatedIntegrationHandler(
                    sp.GetRequiredService<IEventStore>(),
                    sp.GetRequiredService<ICatalogTourSlugLock>()),
                sp.GetRequiredService<IIdempotencyStore>(),
                sp.GetRequiredService<IOptions<IntegrationEventOptions>>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ICatalogInboxDuplicateRecorder>()));
        JsonTypeInfo<AdminTourCreatedIntegrationEvent> adminTourCreatedJsonTypeInfo =
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent;
        services.AddIntegrationEventConsumer(
            AdminTourCreatedIntegrationEvent.EventType,
            adminTourCreatedJsonTypeInfo);
        services.TryAddScoped<IIntegrationEventHandler<AdminTourRetiredIntegrationEvent>, AdminTourRetiredIntegrationHandler>();
        services.AddIntegrationEventConsumer(
            AdminTourRetiredIntegrationEvent.EventType,
            AdminIntegrationEventJsonContext.Default.AdminTourRetiredIntegrationEvent);
        services.TryAddScoped<CatalogTourPresentationService>();
        services.TryAddScoped<CatalogGalleryPlacementQueuedHandler>();
        services.TryAddScoped<IIntegrationEventHandler<CatalogGalleryPlacementQueuedIntegrationEvent>>(provider =>
            new IdempotentIntegrationHandler<CatalogGalleryPlacementQueuedIntegrationEvent>(
                provider.GetRequiredService<CatalogGalleryPlacementQueuedHandler>(),
                provider.GetRequiredService<IIdempotencyStore>(),
                provider.GetRequiredService<IOptions<IntegrationEventOptions>>(),
                provider.GetRequiredService<TimeProvider>(),
                provider.GetRequiredService<ICatalogInboxDuplicateRecorder>()));
        services.AddIntegrationEventConsumer(CatalogGalleryPlacementQueuedIntegrationEvent.EventType,
            CatalogIntegrationEventJsonContext.Default.CatalogGalleryPlacementQueuedIntegrationEvent);
    }
}
