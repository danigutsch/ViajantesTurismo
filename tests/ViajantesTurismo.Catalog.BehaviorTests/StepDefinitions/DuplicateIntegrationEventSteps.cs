using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Catalog.BehaviorTests.StepDefinitions;

[Binding]
public sealed class DuplicateIntegrationEventSteps : IAsyncDisposable
{
    private readonly CatalogIntegrationEventScenario scenario = new();
    private AdminTourCreatedIntegrationEvent? integrationEvent;

    [Given("an Admin tour-created event for identifier {string}")]
    public void Given_an_admin_tour_created_event(string identifier)
    {
        integrationEvent = new AdminTourCreatedIntegrationEvent(Guid.CreateVersion7(),
            new DateTimeOffset(2030, 2, 3, 14, 0, 0, TimeSpan.Zero), Guid.CreateVersion7(), identifier, "Andes adventure");
    }

    [When("Catalog receives that same integration event twice")]
    public async Task When_catalog_receives_the_same_event_twice()
    {
        var message = integrationEvent.ShouldNotBeNull();
        await scenario.Deliver(message, TestContext.Current.CancellationToken);
        await scenario.Deliver(message, TestContext.Current.CancellationToken);
    }

    [Then("Catalog exposes exactly one unchanged draft for that Admin tour")]
    public async Task Then_catalog_exposes_one_unchanged_draft()
    {
        var message = integrationEvent.ShouldNotBeNull();
        var drafts = await scenario.ReadDrafts(TestContext.Current.CancellationToken);
        var draft = drafts.ShouldHaveSingleItem();
        draft.AdminTourId.ShouldBe(message.AdminTourId);
        draft.Identifier.ShouldBe(message.Identifier);
        draft.Title.ShouldBe(message.Name);
        draft.StreamVersion.ShouldBe(1);
        draft.IsPublished.ShouldBeFalse();
    }

    public ValueTask DisposeAsync() => scenario.DisposeAsync();
}
