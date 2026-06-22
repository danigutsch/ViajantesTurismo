using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Tours;

public sealed class CreateTourCommandHandlerTests
{
    [Fact]
    public async Task Handle_dispatches_admin_tour_created_event_after_persisting_the_tour()
    {
        var tourStore = new FakeTourStore();
        var unitOfWork = new FakeUnitOfWork();
        var dispatcher = new CapturingIntegrationEventDispatcher(unitOfWork);
        var handler = new CreateTourCommandHandler(tourStore, unitOfWork, dispatcher);
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

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveEntitiesCallCount);
        var integrationEvent = Assert.IsType<AdminTourCreatedIntegrationEvent>(dispatcher.IntegrationEvent);
        Assert.Equal(result.Value, integrationEvent.AdminTourId);
        Assert.Equal(command.Identifier, integrationEvent.Identifier);
        Assert.Equal(command.Name, integrationEvent.Name);
        Assert.NotEqual(Guid.Empty, integrationEvent.EventId);
        Assert.True(dispatcher.WasDispatchedAfterSave);
    }

    [Fact]
    public async Task AddApplication_dispatcher_invokes_registered_integration_event_handlers()
    {
        using var dispatchScope = TestIntegrationEventDispatchScope.Create();
        var integrationEvent = new TestIntegrationEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        await dispatchScope.Dispatcher.Dispatch(integrationEvent, CancellationToken.None);

        Assert.Same(integrationEvent, dispatchScope.Handler.IntegrationEvent);
    }
}
