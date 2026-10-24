using Microsoft.EntityFrameworkCore;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Branding;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class BrandingProjectionPostgreSqlTests(PostgreSqlTestServerFixture fixture)
{
    [Fact]
    public async Task A_second_worker_with_a_stale_projection_conflicts_and_writes_nothing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await BrandingProjectionPostgreSqlScenario.Create(database, ct);
        await scenario.Apply(BrandingProjectionTestEvents.Create(revision: 1, brandName: "First"), ct);
        var workerA = scenario.CreateStore();
        var workerB = scenario.CreateStore();
        _ = await workerA.GetCurrent(ct);
        _ = await workerB.GetCurrent(ct);
        var fromA = BrandingProjectionTestEvents.Create(revision: 2, brandName: "From A");
        var fromB = BrandingProjectionTestEvents.Create(revision: 3, brandName: "From B");
        workerA.Replace(BrandingProjectionState.From(fromA));
        workerA.Record(new BrandingInboxEntry(fromA.EventId, fromA.SourceEpoch, fromA.SourceRevision, BrandingInboxOutcome.Applied, null, fromA.OccurredAt));
        await workerA.Save(ct);
        workerB.Replace(BrandingProjectionState.From(fromB));
        workerB.Record(new BrandingInboxEntry(fromB.EventId, fromB.SourceEpoch, fromB.SourceRevision, BrandingInboxOutcome.Applied, null, fromB.OccurredAt));

        // Act
        var save = () => workerB.Save(ct);

        // Assert
        await save.ShouldThrow<DbUpdateConcurrencyException>();
        var projection = await scenario.Read(ct);
        projection.ShouldBe((2L, "From A", 2L));
    }

    [Fact]
    public async Task Redelivery_after_a_conflict_applies_the_newer_revision()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await BrandingProjectionPostgreSqlScenario.Create(database, ct);
        await scenario.Apply(BrandingProjectionTestEvents.Create(revision: 1, brandName: "First"), ct);
        await scenario.Apply(BrandingProjectionTestEvents.Create(revision: 2, brandName: "Second"), ct);
        var redelivered = BrandingProjectionTestEvents.Create(revision: 3, brandName: "Third");

        // Act
        await scenario.Apply(redelivered, ct);
        await scenario.Apply(redelivered, ct);

        // Assert
        var projection = await scenario.Read(ct);
        projection.ShouldBe((3L, "Third", 3L));
    }
}
