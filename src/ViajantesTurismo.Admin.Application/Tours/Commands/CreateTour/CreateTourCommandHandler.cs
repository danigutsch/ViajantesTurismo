using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Tours.Commands.CreateTour;

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

        var errors = new ValidationErrors();

        if (await tourStore.IdentifierExists(command.Identifier, ct))
        {
            errors.Add(TourErrors.IdentifierAlreadyExists(command.Identifier));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Guid>();
        }

        var currency = TourMapper.MapToCurrency(command.Currency);

        var tourResult = Tour.Create(
            command.Identifier,
            command.Name,
            command.StartDate.ToUniversalTime(),
            command.EndDate.ToUniversalTime(),
            command.Price,
            command.DoubleRoomSupplementPrice,
            command.RegularBikePrice,
            command.EBikePrice,
            currency,
            command.MinCustomers,
            command.MaxCustomers,
            [.. command.IncludedServices]);

        if (!tourResult.IsSuccess)
        {
            return tourResult.ConvertError<Tour, Guid>();
        }

        tourStore.Add(tourResult.Value);
        await unitOfWork.SaveEntities(ct);

        return Result<Guid>.Ok(tourResult.Value.Id);
    }
}
