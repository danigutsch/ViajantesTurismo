using PandaBear.Mediator;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Tours.CreateTour;

/// <summary>
/// Command to create a new tour.
/// </summary>
public sealed record CreateTourCommand(
    string Identifier,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Price,
    decimal SingleRoomSupplementPrice,
    decimal RegularBikePrice,
    decimal EBikePrice,
    string Currency,
    ICollection<string> IncludedServices,
    int MinCustomers,
    int MaxCustomers) : ICommand<Result<Guid>>;
