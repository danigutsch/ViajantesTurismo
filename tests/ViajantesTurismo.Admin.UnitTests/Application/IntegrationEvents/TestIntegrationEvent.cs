using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

internal sealed record TestIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public static string EventType => "test.integration-event";

    public static int EventVersion => 1;
}
