using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Tours;

public sealed class CreateTourCommandHandlerTests
{
    [Fact]
    public async Task Handle_persists_the_created_tour()
    {
        var tourStore = new FakeTourStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateTourCommandHandler(tourStore, unitOfWork);
        var command = new CreateTourCommand(
            "andes-2026",
            "Andes 2026",
            new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            1_500m,
            250m,
            100m,
            180m,
            CurrencyDto.Euro,
            ["Guide"],
            4,
            12);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
        var storedTour = (await tourStore.GetById(result.Value, CancellationToken.None)).ShouldNotBeNull();
        storedTour.Id.ShouldBe(result.Value);
        storedTour.Identifier.ShouldBe(command.Identifier);
        storedTour.Name.ShouldBe(command.Name);
    }
}
