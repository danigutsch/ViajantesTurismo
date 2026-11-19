using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Tours.DeleteTour;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;
using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Admin.UnitTests.Application.Tours.DeleteTour;

/// <summary>Checks retirement eligibility and the retained Tour record.</summary>
[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
public sealed class DeleteTourCommandHandlerTests
{
    private static readonly DateTimeOffset RequestedAt = new(2026, 11, 19, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Retirement_preserves_the_tour_record()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var store = new FakeTourStore();
        var tour = EntityBuilders.BuildTour();
        store.Add(tour);
        var handler = new DeleteTourCommandHandler(store, new FakeUnitOfWork());

        // Act
        var result = await handler.Handle(new DeleteTourCommand(tour.Id, "admin-subject", RequestedAt), ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var retained = await store.GetById(tour.Id, ct);
        retained.ShouldBeSameAs(tour);
        tour.RetiredAt.ShouldBe(RequestedAt);
        tour.RetirementId.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retirement_conflicts_with_an_active_booking(bool confirmed)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var store = new FakeTourStore();
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        if (confirmed)
        {
            tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        }
        store.Add(tour);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new DeleteTourCommandHandler(store, unitOfWork);

        // Act
        var result = await handler.Handle(new DeleteTourCommand(tour.Id, "admin-subject", RequestedAt), ct);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        tour.IsRetired.ShouldBeFalse();
    }

    [Fact]
    public async Task Repeated_retirement_keeps_the_original_marker_and_saves_once()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var store = new FakeTourStore();
        var tour = EntityBuilders.BuildTour();
        store.Add(tour);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new DeleteTourCommandHandler(store, unitOfWork);

        // Act
        var first = await handler.Handle(new DeleteTourCommand(tour.Id, "original-actor", RequestedAt), ct);
        var marker = tour.RetirementId;
        var second = await handler.Handle(new DeleteTourCommand(tour.Id, "another-actor", RequestedAt.AddMinutes(1)), ct);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        tour.RetirementId.ShouldBe(marker);
        tour.RetiredAt.ShouldBe(RequestedAt);
        var retirement = tour.GetDomainEvents().OfType<TourRetiredDomainEvent>().ShouldHaveSingleItem();
        retirement.RetirementId.ShouldBe(marker);
        retirement.ActorId.ShouldBe("original-actor");
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
