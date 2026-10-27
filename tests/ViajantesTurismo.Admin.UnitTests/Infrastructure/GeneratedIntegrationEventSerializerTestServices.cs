using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>
/// Builds integration event serializers backed by the generated admin event contract for tests.
/// </summary>
internal static class GeneratedIntegrationEventSerializerTestServices
{
    /// <summary>
    /// Creates the serializer resolved from a provider with the admin integration event contract registered.
    /// </summary>
    public static IIntegrationEventSerializer CreateSerializer()
    {
        var services = new ServiceCollection();
        services.AddAdminIntegrationEventContract();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IIntegrationEventSerializer>();
    }

    /// <summary>
    /// Registers the admin tour-created contract and the generated integration events.
    /// </summary>
    public static IServiceCollection AddAdminIntegrationEventContract(this IServiceCollection services)
    {
        JsonTypeInfo<AdminTourCreatedIntegrationEvent> adminTourCreatedJsonTypeInfo =
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent;
        services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            adminTourCreatedJsonTypeInfo);
        services.AddGeneratedIntegrationEvents();

        return services;
    }
}
