using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.IntegrationEvents;
using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

internal sealed class TestIntegrationEventDispatchScope(
    ServiceProvider serviceProvider,
    IIntegrationEventDispatcher dispatcher,
    CapturingTestIntegrationEventHandler handler) : IDisposable
{
    public IIntegrationEventDispatcher Dispatcher => dispatcher;

    public CapturingTestIntegrationEventHandler Handler => handler;

    public static TestIntegrationEventDispatchScope Create()
    {
        var builder = Host.CreateApplicationBuilder();
        var handler = new CapturingTestIntegrationEventHandler();
        builder.AddApplication();
        builder.Services.AddSingleton<IIntegrationEventHandler<TestIntegrationEvent>>(handler);
        var serviceProvider = builder.Services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IIntegrationEventDispatcher>();

        return new TestIntegrationEventDispatchScope(serviceProvider, dispatcher, handler);
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
