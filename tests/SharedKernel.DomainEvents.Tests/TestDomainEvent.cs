using SharedKernel.Domain;

namespace SharedKernel.DomainEvents.Tests;

internal sealed record TestDomainEvent(string Name) : IDomainEvent;
