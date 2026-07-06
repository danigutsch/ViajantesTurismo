using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Contracts.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class RegisteredIntegrationEventSerializerTestServices
{
    public static IIntegrationEventSerializer CreateSerializer()
    {
        var services = new ServiceCollection();
        services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IIntegrationEventSerializer>();
    }

    public static IServiceCollection AddAdminIntegrationEventContract(this IServiceCollection services)
    {
        services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent);

        return services;
    }
}
