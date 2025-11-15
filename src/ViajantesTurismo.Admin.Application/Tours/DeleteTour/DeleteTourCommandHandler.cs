using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Tours.DeleteTour;

/// <summary>
/// Handles the deletion of a tour with business rule validation.
/// </summary>
public sealed class DeleteTourCommandHandler(
    ITourStore tourStore,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Handles the DeleteTourCommand and deletes the tour if business rules allow.
    /// </summary>
    /// <param name="command">The command containing the tour ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or validation errors.</returns>
    public async Task<Result> Handle(DeleteTourCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tour = await tourStore.GetById(command.TourId, ct);
        if (tour is null)
        {
            return TourErrors.TourNotFound(command.TourId).ConvertError();
        }

        var deleteResult = tour.Delete();
        if (deleteResult.IsFailure)
        {
            return deleteResult;
        }

        tourStore.Delete(tour);
        await unitOfWork.SaveEntities(ct);

        return Result.Ok();
    }
}
