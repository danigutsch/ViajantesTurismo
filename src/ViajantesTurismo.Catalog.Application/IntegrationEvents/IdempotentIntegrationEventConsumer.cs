using SharedKernel.Idempotency;
using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Application.IntegrationEvents;

/// <summary>
/// Applies inbox-style idempotency around Catalog integration event consumers.
/// </summary>
/// <typeparam name="TIntegrationEvent">The integration event type.</typeparam>
public sealed class IdempotentIntegrationEventConsumer<TIntegrationEvent>(
    IIntegrationEventHandler<TIntegrationEvent> inner,
    IIdempotencyStore idempotencyStore) : IIntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    private static readonly IdempotencyScope Scope = IdempotencyScope.From(
        $"catalog.integration-event.{typeof(TIntegrationEvent).Name}");

    /// <inheritdoc />
    public async ValueTask Handle(TIntegrationEvent notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var operation = new IdempotencyOperation(Scope, IdempotencyKey.From(notification.EventId.ToString("N")));
        var startResult = await idempotencyStore.TryStart(
            operation,
            DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(5),
            ct);
        if (!startResult.Started)
        {
            return;
        }

        await inner.Handle(notification, ct);
        await idempotencyStore.Complete(operation, DateTimeOffset.UtcNow, notification.EventId.ToString("N"), ct);
    }
}
