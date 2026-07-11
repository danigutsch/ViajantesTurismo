using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class TourDomainEventTests
{
    [Fact]
    public void Create_records_tour_created_domain_event()
    {
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "andes-2026", Name: "Andes 2026"));

        var domainEvent = tour.GetDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<TourCreatedDomainEvent>();

        domainEvent.TourId.ShouldBe(tour.Id);
        domainEvent.Identifier.ShouldBe(tour.Identifier);
        domainEvent.Name.ShouldBe(tour.Name);
    }

    [Fact]
    public void ClearDomainEvents_removes_recorded_domain_events()
    {
        var tour = EntityBuilders.BuildTour();

        tour.ClearDomainEvents();

        tour.GetDomainEvents().ShouldBeEmpty();
    }
}
