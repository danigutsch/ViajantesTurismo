# SharedKernel.DomainEvents

Typed domain event dispatch abstractions for in-process bounded-context events.

Domain events remain separate from integration events. Generated modules contribute closed
`IDomainEventDispatchHandler` implementations, and the scoped `CompositeDomainEventDispatcher`
invokes each applicable handler. Provider modules can therefore compose transactional outbox mappings,
audit mappings, and other typed behavior without a runtime registry or mediator adapter.
