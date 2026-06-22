using SharedKernel.Domain;

namespace SharedKernel.DomainEvents;

/// <summary>
/// Handles a domain event raised inside one bounded context.
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type handled by the handler.</typeparam>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Handles the provided domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event instance to handle.</param>
    /// <param name="ct">The cancellation token for the operation.</param>
    /// <returns>A task that completes when handling finishes.</returns>
    ValueTask Handle(TDomainEvent domainEvent, CancellationToken ct);
}
