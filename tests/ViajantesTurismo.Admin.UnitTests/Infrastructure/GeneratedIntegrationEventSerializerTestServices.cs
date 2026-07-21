using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class GeneratedIntegrationEventSerializerTestServices
{
    public static IIntegrationEventSerializer CreateSerializer()
    {
        var services = new ServiceCollection();
        services.AddAdminIntegrationEventContract();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IIntegrationEventSerializer>();
    }

    public static IServiceCollection AddAdminIntegrationEventContract(this IServiceCollection services)
    {
        services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent);
        services.AddDomainEventProcessing();

        return services;
    }
}
