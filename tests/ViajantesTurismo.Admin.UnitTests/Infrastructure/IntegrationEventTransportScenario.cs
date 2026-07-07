using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Messaging;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class IntegrationEventTransportScenario(ServiceProvider provider) : IAsyncDisposable
{
    private readonly AsyncServiceScope scenarioScope = provider.CreateAsyncScope();

    public CatalogIntegrationTransportDbContext DbContext => scenarioScope.ServiceProvider.GetRequiredService<CatalogIntegrationTransportDbContext>();

    public IEventEnvelopePublisher Publisher => scenarioScope.ServiceProvider.GetRequiredService<IEventEnvelopePublisher>();

    public async ValueTask DisposeAsync()
    {
        await scenarioScope.DisposeAsync();
        await provider.DisposeAsync();
    }
}
