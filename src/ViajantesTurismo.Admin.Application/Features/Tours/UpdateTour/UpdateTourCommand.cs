using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Application.Tours.Commands.UpdateTour;

/// <summary>
/// Command to update an existing tour's details, schedule, pricing, and included services.
/// </summary>
public sealed record UpdateTourCommand(
    Guid TourId,
    string Identifier,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    decimal Price,
    decimal DoubleRoomSupplementPrice,
    decimal RegularBikePrice,
    decimal EBikePrice,
    Currency Currency,
    IReadOnlyCollection<string> IncludedServices,
    int MinCustomers,
    int MaxCustomers);
