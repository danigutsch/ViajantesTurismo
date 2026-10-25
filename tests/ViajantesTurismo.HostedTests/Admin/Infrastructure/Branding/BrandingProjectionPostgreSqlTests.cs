using Microsoft.EntityFrameworkCore;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Branding;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class BrandingProjectionPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
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

    [Fact]
    public async Task Epoch_rotation_recovery_fences_reseeds_once_and_quarantines_the_retired_lineage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var scenario = await BrandingProjectionPostgreSqlScenario.Create(database, ct);
        await scenario.Apply(BrandingProjectionTestEvents.Create(revision: 3, brandName: "Before restore"), ct);
        var newEpoch = Guid.CreateVersion7();
        var reseed = BrandingProjectionTestEvents.Create(revision: 1, brandName: "Restored", epoch: newEpoch);
        var unauthorized = () => scenario.Apply(reseed, ct);
        await unauthorized.ShouldThrow<BrandingProjectionRecoveringException>();
        var authorization = await scenario.CreateRecoveryService().AuthorizeReseed(
            new BrandingReseedRequest(BrandingProjectionTestEvents.Epoch, 3, reseed.EventId, newEpoch, 1, "restore", "INC-7", "operator@example.test"),
            ct);

        // Act
        await scenario.Apply(reseed, ct);
        await scenario.Apply(BrandingProjectionTestEvents.Create(revision: 4, brandName: "Old lineage"), ct);

        // Assert
        authorization.IsSuccess.ShouldBeTrue();
        var projection = await scenario.Read(ct);
        projection.ShouldBe((1L, "Restored", 3L));
        var recovery = await scenario.ReadRecovery(ct);
        recovery.ShouldBe(("Ready", 1L));
    }
}
