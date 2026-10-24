using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Tours.UpdateTour;

/// <summary>
/// Handles the update of an existing tour with validation for uniqueness and business rules.
/// </summary>
public sealed class UpdateTourCommandHandler(
    ITourStore tourStore,
    ITourCapacityMutationLock capacityMutationLock,
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

        await using var capacityLease = await capacityMutationLock.Acquire(command.TourId, ct);
        var tour = await tourStore.GetById(command.TourId, ct);
        if (tour is null)
        {
            return TourErrors.TourNotFound(command.TourId);
        }

        if (await tourStore.IdentifierExistsExcluding(command.Identifier, command.TourId, ct))
        {
            return TourErrors.IdentifierAlreadyExists(command.Identifier);
        }

        var reviseResult = tour.Revise(new TourDefinition(
            command.Identifier,
            command.Name,
            command.StartDate,
            command.EndDate,
            command.Price,
            command.SingleRoomSupplementPrice,
            command.RegularBikePrice,
            command.EBikePrice,
            command.Currency,
            command.MinCustomers,
            command.MaxCustomers,
            command.IncludedServices));
        if (reviseResult.IsFailure)
        {
            return reviseResult;
        }

        await unitOfWork.SaveEntities(ct);

        return Result.Ok();
    }
}
