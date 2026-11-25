using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Tours.DeleteTour;

/// <summary>Retires the locked tour and saves its transition in the mediator transaction.</summary>
public sealed class DeleteTourCommandHandler(
    ITourCommandStore tourStore,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteTourCommand, Result>
{
    /// <inheritdoc />
    public async ValueTask<Result> Handle(DeleteTourCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var storedTour = await tourStore.GetById(command.TourId, ct);
        if (!storedTour.TryGetValue(out var tour))
        {
            return TourErrors.TourNotFound();
        }

        var retirement = tour.Retire(command.RetirementId, command.ActorId, command.RequestedAt);
        if (retirement.IsFailure)
        {
            return retirement.ConvertError();
        }

        if (retirement.Value)
        {
            await unitOfWork.SaveEntities(ct);
        }

        return Result.Ok();
    }
}
