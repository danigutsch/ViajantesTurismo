using SharedKernel.Domain;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Raised when a tour is created inside the Admin bounded context.
/// </summary>
/// <param name="TourId">The created tour identifier.</param>
/// <param name="Identifier">The created tour business identifier.</param>
/// <param name="Name">The created tour name.</param>
public sealed record TourCreatedDomainEvent(
    Guid TourId,
    string Identifier,
    string Name) : IDomainEvent;
