using PandaBear.Mediator;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Tours.DeleteTour;

/// <summary>Requests retirement of the active Tour resource.</summary>
/// <param name="TourId">The tour identifier.</param>
/// <param name="ActorId">The authenticated opaque actor identifier.</param>
/// <param name="RequestedAt">The server-captured UTC request instant.</param>
public sealed record DeleteTourCommand(Guid TourId, string ActorId, DateTimeOffset RequestedAt) : ICommand<Result>
{
    /// <summary>The retirement identifier reused across transaction attempts.</summary>
    internal Guid RetirementId { get; } = Guid.CreateVersion7();
}
