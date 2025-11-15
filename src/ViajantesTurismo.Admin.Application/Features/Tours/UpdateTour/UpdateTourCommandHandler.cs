using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Features.Tours.UpdateTour;

/// <summary>
/// Handles the update of an existing tour with validation for uniqueness and business rules.
/// </summary>
public sealed class UpdateTourCommandHandler(
    ITourStore tourStore,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Handles the UpdateTourCommand and updates the tour if validation passes.
    /// </summary>
    /// <param name="command">The command containing the tour ID and updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or validation errors.</returns>
    public async Task<Result> Handle(UpdateTourCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tour = await tourStore.GetById(command.TourId, ct);
        if (tour is null)
        {
            return TourErrors.TourNotFound(command.TourId).ConvertError();
        }

        if (await tourStore.IdentifierExistsExcluding(command.Identifier, command.TourId, ct))
        {
            return TourErrors.IdentifierAlreadyExists(command.Identifier);
        }

        var detailsResult = tour.UpdateDetails(command.Identifier, command.Name);
        if (detailsResult.IsFailure)
        {
            return detailsResult;
        }

        var scheduleResult = tour.UpdateSchedule(command.StartDate, command.EndDate);
        if (scheduleResult.IsFailure)
        {
            return scheduleResult;
        }

        var pricingResult = tour.UpdatePricing(
            command.DoubleRoomSupplementPrice,
            command.RegularBikePrice,
            command.EBikePrice,
            command.Currency);
        if (pricingResult.IsFailure)
        {
            return pricingResult;
        }

        var basePriceResult = tour.UpdateBasePrice(command.Price);
        if (basePriceResult.IsFailure)
        {
            return basePriceResult;
        }

        var capacityResult = tour.UpdateCapacity(command.MinCustomers, command.MaxCustomers);
        if (capacityResult.IsFailure)
        {
            return capacityResult;
        }

        tour.UpdateIncludedServices([.. command.IncludedServices]);

        await unitOfWork.SaveEntities(ct);

        return Result.Ok();
    }
}
