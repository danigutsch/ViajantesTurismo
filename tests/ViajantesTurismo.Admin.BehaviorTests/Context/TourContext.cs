using JetBrains.Annotations;
using ViajantesTurismo.Admin.Application.Features.Tours.CreateTour;
using ViajantesTurismo.Admin.Application.Features.Tours.DeleteTour;
using ViajantesTurismo.Admin.Application.Features.Tours.UpdateTour;
using ViajantesTurismo.Admin.BehaviorTests.Fakes;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class TourContext
{
    public required string Identifier { get; set; }
    public required string Name { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required decimal BasePrice { get; set; }
    public required decimal DoubleRoomSupplementPrice { get; set; }
    public required decimal RegularBikePrice { get; set; }
    public required decimal EBikePrice { get; set; }
    public ICollection<string> IncludedServices { get; } = [];
    public required Tour Tour { get; set; }
    public required object Result { get; set; }
    public Result? UpdateResult { get; set; }
    public Result<Booking>? BookingResult { get; set; }

    public FakeTourStore TourStore { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; } = new();
    public CreateTourCommandHandler CreateTourCommandHandler => new(TourStore, UnitOfWork);
    public DeleteTourCommandHandler DeleteTourCommandHandler => new(TourStore, UnitOfWork);
    public UpdateTourCommandHandler UpdateTourCommandHandler => new(TourStore, UnitOfWork);
    public Result<Guid>? CommandResult { get; set; }
    public Result? DeleteResult { get; set; }
}
