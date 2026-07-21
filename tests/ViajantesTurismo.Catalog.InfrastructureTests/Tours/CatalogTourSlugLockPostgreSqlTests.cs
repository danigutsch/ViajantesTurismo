using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.InfrastructureTests.Tours;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class CatalogTourSlugLockPostgreSqlTests : IAsyncLifetime
{
    private CatalogTourSlugLockPostgreSqlScenario? scenario;

    private CatalogTourSlugLockPostgreSqlScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await CatalogTourSlugLockPostgreSqlScenario.Create(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var currentScenario = scenario;
        scenario = null;

        if (currentScenario is not null)
        {
            await currentScenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task Slug_claim_completes_when_the_event_store_pool_has_one_connection()
    {
        // Arrange
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        // Act
        await Scenario.Handle(integrationEvent, timeout.Token);
        var events = await Scenario.Load(
            CatalogTourStreamIds.FromAdminTourId(integrationEvent.AdminTourId),
            TestContext.Current.CancellationToken);

        // Assert
        var envelope = events.ShouldHaveSingleItem();
        var draftCreated = envelope.Data.ShouldBeOfType<CatalogTourDraftCreated>();
        draftCreated.AdminTourId.ShouldBe(integrationEvent.AdminTourId);
    }
}
