using System.Text.Json;
using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class EfIntegrationEventOutbox(AdminWriteDbContext dbContext, TimeProvider timeProvider) : IIntegrationEventOutbox
{
    public ValueTask Enqueue<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken ct)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ct.ThrowIfCancellationRequested();

        dbContext.Set<IntegrationEventOutboxMessage>().Add(new IntegrationEventOutboxMessage
        {
            Id = Guid.CreateVersion7(),
            EventType = TIntegrationEvent.EventType,
            EventVersion = TIntegrationEvent.EventVersion,
            EventId = integrationEvent.EventId,
            OccurredAt = integrationEvent.OccurredAt,
            PayloadJson = JsonSerializer.Serialize(integrationEvent),
            EnqueuedAt = timeProvider.GetUtcNow(),
        });

        return ValueTask.CompletedTask;
    }
}
