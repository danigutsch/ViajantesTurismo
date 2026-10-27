using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>
/// Integration event with an event type no contract registers, used to test unknown-event handling.
/// </summary>
/// <param name="EventId">The unique event identifier.</param>
/// <param name="OccurredAt">When the event occurred.</param>
internal sealed record UnknownAdminIntegrationEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    /// <summary>
    /// Gets the unregistered event type name.
    /// </summary>
    public static string EventType => "admin.unknown";

    /// <summary>
    /// Gets the event schema version.
    /// </summary>
    public static int EventVersion => 1;
}
