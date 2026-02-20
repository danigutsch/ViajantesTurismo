using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Application.Tours.CreateTour;

/// <summary>
/// Command to create a new tour.
/// </summary>
public sealed record CreateTourCommand(
    string Identifier,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    decimal Price,
    decimal SingleRoomSupplementPrice,
    decimal RegularBikePrice,
    decimal EBikePrice,
    CurrencyDto Currency,
    ICollection<string> IncludedServices,
    int MinCustomers,
    int MaxCustomers);
