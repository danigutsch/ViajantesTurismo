using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Application.Tours.UpdateTour;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Tours;

/// <summary>Protects the ISO 4217 validation of tour currency codes at the application boundary.</summary>
public sealed class TourCurrencyCommandHandlerTests
{
    [Theory]
    [InlineData("JPY")]
    [InlineData("GBP")]
    [InlineData("Euro")]
    [InlineData("E1R")]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Creating_a_tour_rejects_unsupported_and_malformed_currency_codes_with_a_currency_error(string code)
    {
        // Arrange
        var tourStore = new FakeTourStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateTourCommandHandler(tourStore, unitOfWork);

        // Act
        var result = await handler.Handle(TourCommandFactory.Create(code), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(["currency"]);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
        var stored = await tourStore.IdentifierExists("andes-2026", CancellationToken.None);
        stored.ShouldBeFalse();
    }

    [Theory]
    [InlineData("BRL", "BRL")]
    [InlineData("eur", "EUR")]
    [InlineData(" usd ", "USD")]
    public async Task Creating_a_tour_stores_the_normalized_iso_code(string code, string expected)
    {
        // Arrange
        var tourStore = new FakeTourStore();
        var handler = new CreateTourCommandHandler(tourStore, new FakeUnitOfWork());

        // Act
        var result = await handler.Handle(TourCommandFactory.Create(code), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var stored = await tourStore.GetById(result.Value, CancellationToken.None);
        stored.ShouldNotBeNull().Pricing.Currency.Code.ShouldBe(expected);
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("Real")]
    [InlineData("E1R")]
    public async Task Updating_a_tour_rejects_unsupported_and_malformed_currency_codes_with_a_currency_error(string code)
    {
        // Arrange
        var tourStore = new FakeTourStore();
        var tour = EntityBuilders.BuildTour();
        tourStore.Add(tour);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateTourCommandHandler(tourStore, new NoOpTourCapacityMutationLock(), unitOfWork);

        // Act
        var result = await handler.Handle(TourCommandFactory.Update(tour.Id, tour.Pricing.BasePrice, code), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldBe(["currency"]);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Updating_a_tour_stores_the_normalized_iso_code()
    {
        // Arrange
        var tourStore = new FakeTourStore();
        var tour = EntityBuilders.BuildTour();
        tourStore.Add(tour);
        var handler = new UpdateTourCommandHandler(tourStore, new NoOpTourCapacityMutationLock(), new FakeUnitOfWork());

        // Act
        var result = await handler.Handle(TourCommandFactory.Update(tour.Id, tour.Pricing.BasePrice, "brl"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        tour.Pricing.Currency.Code.ShouldBe("BRL");
    }

}
