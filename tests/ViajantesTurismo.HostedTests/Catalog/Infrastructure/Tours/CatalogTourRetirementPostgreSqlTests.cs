using Microsoft.EntityFrameworkCore;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Exercises the Catalog retirement fence against the PostgreSQL stores.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogTourRetirementPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task A_failed_completion_insert_leaves_public_withdrawal_pending_until_delivery_retries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogTourRetirementPostgreSqlScenario.Create(database.ConnectionString, ct);
        var adminTourId = Guid.CreateVersion7();
        var retirement = new AdminTourRetiredIntegrationEvent(
            Guid.CreateVersion7(), TimeProvider.System.GetUtcNow(), adminTourId, Guid.CreateVersion7());
        await scenario.InjectOutboxFailure(ct);

        // Act
        Func<Task> firstAttempt = () => scenario.Retire(retirement, ct);
        await firstAttempt.ShouldThrow<DbUpdateException>();
        var (markersBeforeRetry, eventsBeforeRetry) = await scenario.ReadCompletion(adminTourId, ct);
        await scenario.RemoveOutboxFailure(ct);
        await scenario.Retire(retirement, ct);
        var (markersAfterRetry, eventsAfterRetry) = await scenario.ReadCompletion(adminTourId, ct);

        // Assert
        markersBeforeRetry.ShouldBe(0);
        eventsBeforeRetry.ShouldBeEmpty();
        markersAfterRetry.ShouldBe(1);
        eventsAfterRetry.ShouldHaveSingleItem().ShouldBe(retirement.RetirementId.ToString("D"));
    }

    [Fact]
    public async Task Retirement_withdraws_public_visibility_even_when_a_stale_publication_projects_later()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogTourRetirementPostgreSqlScenario.Create(database.ConnectionString, ct);
        await using var context = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString(database.ConnectionString);
        var store = new EfCatalogTourReadModelStore(context);
        var now = TimeProvider.System.GetUtcNow();
        var tour = new CatalogTourDraftReadModel(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "andes-tour", "Andes", "andes", true, 1, now);
        await store.UpsertDraft(tour, ct);
        var previouslyPublished = await store.GetPublishedTourBySlug(tour.Slug, ct);
        previouslyPublished.ShouldNotBeNull();
        var retirementId = Guid.CreateVersion7();
        var retirement = new AdminTourRetiredIntegrationEvent(Guid.CreateVersion7(), now, tour.AdminTourId, retirementId);

        // Act
        await Task.WhenAll(scenario.Retire(retirement, ct), scenario.Retire(retirement, ct));
        _ = await store.SetPublicationStatus(tour.CatalogTourId, true, 2, 2, now, ct);
        var publicTour = await store.GetPublishedTourBySlug(tour.Slug, ct);
        var managementTour = await store.GetTour(tour.CatalogTourId, ct);
        var list = await store.ListTours(ct);
        var (markerCount, eventIds) = await scenario.ReadCompletion(tour.AdminTourId, ct);

        // Assert
        publicTour.ShouldBeNull();
        managementTour.ShouldNotBeNull().IsPubliclyVisible.ShouldBeFalse();
        list.ShouldHaveSingleItem().IsPubliclyVisible.ShouldBeFalse();
        markerCount.ShouldBe(1);
        eventIds.ShouldHaveSingleItem().ShouldBe(retirementId.ToString("D"));
    }
}
