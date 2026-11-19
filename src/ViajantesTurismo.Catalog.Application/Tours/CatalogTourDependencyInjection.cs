using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PandaBear.EventSourcing;
using PandaBear.Idempotency;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;

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
                sp.GetRequiredService<TimeProvider>()));
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
    }
}
