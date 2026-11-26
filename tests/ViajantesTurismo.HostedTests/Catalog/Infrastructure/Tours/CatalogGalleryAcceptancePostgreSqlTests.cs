using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies that Gallery retains its original acceptance time independently of delivery retention.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryAcceptancePostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Read_retains_the_original_acceptance_time_after_the_wake_is_removed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        await using var source = NpgsqlDataSource.Create(database.ConnectionString);
        await CatalogGalleryProcessStatusIndexes.EnsureIndexes(source, ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var photo = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
        await scenario.Images.Upsert(photo, ct);
        var queued = await scenario.Writer.QueueGallery(tour.Id, [photo.Id], photo.Id, 1, 0, canPublish: false, ct);
        var sequence = queued.Value;
        var intent = await scenario.Intents.GetIntent(tour.Id, sequence, ct);
        var wake = await scenario.GetWake(intent.ShouldNotBeNull().Id, ct);

        // Act
        var pending = await scenario.ProcessStatus.Read(tour.Id, sequence, ct);
        var storedAcceptance = await scenario.Database.Database.SqlQuery<DateTimeOffset?>($"""
            SELECT "AcceptedAtUtc" AS "Value" FROM "CatalogGalleryPlacementIntents"
            WHERE "CatalogTourId" = {tour.Id} AND "Sequence" = {sequence}
            """).SingleAsync(ct);
        await scenario.Handler.Handle(wake, ct);
        var removed = await scenario.Database.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM messaging.outbox_messages WHERE "EventId" = {wake.EventId.ToString("D")}
            """, ct);
        var applied = await scenario.ProcessStatus.Read(tour.Id, sequence, ct);

        // Assert
        var expected = wake.OccurredAt.AddTicks(-(wake.OccurredAt.Ticks % 10));
        storedAcceptance.ShouldBe(expected);
        var queuedStatus = pending.ShouldNotBeNull();
        queuedStatus.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        queuedStatus.AcceptedAt.ShouldBe(expected);
        queuedStatus.RecordedAt.ShouldBeNull();
        removed.ShouldBe(1);
        var appliedStatus = applied.ShouldNotBeNull();
        appliedStatus.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        appliedStatus.AcceptedAt.ShouldBe(expected);
        appliedStatus.RecordedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Read_leaves_acceptance_unknown_for_a_legacy_intent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var migrator = scenario.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261123152104_AddCatalogGalleryOptionSearchUnaccent", ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var intentId = Guid.CreateVersion7();
        var photos = Array.Empty<Guid>();
        await scenario.Database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "CatalogGalleryPlacementIntents"
                ("Id", "CatalogTourId", "Sequence", "PhotoIds", "CanPublish", "AppendsUploadedPhoto")
            VALUES ({intentId}, {tour.Id}, 1, {photos}, false, false)
            """, ct);

        // Act
        await migrator.MigrateAsync(targetMigration: null, ct);
        await using var source = NpgsqlDataSource.Create(database.ConnectionString);
        await CatalogGalleryProcessStatusIndexes.EnsureIndexes(source, ct);
        var read = await scenario.ProcessStatus.Read(tour.Id, 1, ct);

        // Assert
        var status = read.ShouldNotBeNull();
        status.IntentId.ShouldBe(intentId);
        status.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        status.AcceptedAt.ShouldBeNull();
    }
}
