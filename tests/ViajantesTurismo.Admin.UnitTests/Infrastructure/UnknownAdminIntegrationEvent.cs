using SharedKernel.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed record UnknownAdminIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public static string EventType => "admin.unknown";

    public static int EventVersion => 1;
}
