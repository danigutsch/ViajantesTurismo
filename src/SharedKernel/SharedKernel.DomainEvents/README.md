# SharedKernel.DomainEvents

Typed domain event dispatch abstractions for in-process bounded-context events.

Domain events remain separate from integration events and are dispatched through the existing
`SharedKernel.Mediator` notification model.
