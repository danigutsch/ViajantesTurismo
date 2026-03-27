using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Tours.CreateTour;

/// <summary>
/// Handles the creation of a new tour with application-level validation.
/// </summary>
public sealed class CreateTourCommandHandler(
    ITourStore tourStore,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Handles the CreateTourCommand and returns the ID of the created tour.
    /// </summary>
    /// <param name="command">The command containing tour data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the tour ID if successful, or validation errors.</returns>
    public async Task<Result<Guid>> Handle(CreateTourCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await tourStore.IdentifierExists(command.Identifier, ct))
        {
            return TourErrors.IdentifierAlreadyExists(command.Identifier).ConvertError<Guid>();
        }

        var currency = TourMapper.MapToCurrency(command.Currency);

        var tourResult = Tour.Create(new TourDefinition(
            command.Identifier,
            command.Name,
            new TourScheduleDefinition(command.StartDate, command.EndDate),
            new TourPricingDefinition(
                command.Price,
                command.SingleRoomSupplementPrice,
                command.RegularBikePrice,
                command.EBikePrice,
                currency),
            new TourCapacityDefinition(command.MinCustomers, command.MaxCustomers),
            [.. command.IncludedServices]));

        if (!tourResult.IsSuccess)
        {
            return tourResult.ConvertError<Tour, Guid>();
        }

        tourStore.Add(tourResult.Value);
        await unitOfWork.SaveEntities(ct);

        return Result<Guid>.Ok(tourResult.Value.Id);
    }
}
