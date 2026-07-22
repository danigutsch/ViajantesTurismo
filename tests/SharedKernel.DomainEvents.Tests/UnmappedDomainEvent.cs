using SharedKernel.Domain;

namespace SharedKernel.DomainEvents.Tests;

internal sealed record UnmappedDomainEvent(string Name) : IDomainEvent;
