using JetBrains.Annotations;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Application.Tours.DeleteTour;
using ViajantesTurismo.Admin.Application.Tours.UpdateTour;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class TourContext
{
    public required string Identifier { get; set; }
    public required string Name { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required decimal BasePrice { get; set; }
    public required decimal SingleRoomSupplementPrice { get; set; }
    public required decimal RegularBikePrice { get; set; }
    public required decimal EBikePrice { get; set; }
    public ICollection<string> IncludedServices { get; } = [];
    public required Tour Tour { get; set; }

    public Result<Tour>? CreationResult { get; set; }
    public Result? CapacityUpdateResult { get; set; }
    public Result? UpdateResult { get; set; }

    public FakeTourStore TourStore { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; } = new();
    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(2026, 6, 22, 12, 30, 0, TimeSpan.Zero));
    public CreateTourCommandHandler CreateTourCommandHandler => new(
        TourStore,
        UnitOfWork,
        IntegrationEventDispatcher,
        TimeProvider);
    public DeleteTourCommandHandler DeleteTourCommandHandler => new(TourStore, UnitOfWork);
    public UpdateTourCommandHandler UpdateTourCommandHandler => new(TourStore, UnitOfWork);
    public CapturingIntegrationEventDispatcher IntegrationEventDispatcher { get; } = new();
    public Result<Guid>? CommandResult { get; set; }
    public Result? DeleteResult { get; set; }
}
