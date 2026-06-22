using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

internal sealed class CapturingTestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
{
    public TestIntegrationEvent? IntegrationEvent { get; private set; }

    public ValueTask Handle(TestIntegrationEvent notification, CancellationToken ct)
    {
        IntegrationEvent = notification;

        return ValueTask.CompletedTask;
    }
}
