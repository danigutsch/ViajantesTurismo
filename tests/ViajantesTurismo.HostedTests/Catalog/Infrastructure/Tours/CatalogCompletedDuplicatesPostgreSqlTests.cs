using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SharedKernel.EventSourcing;
using SharedKernel.Idempotency;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies how the persisted Catalog inbox distinguishes live leases and completed deliveries.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogCompletedDuplicatesPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task A_completed_gallery_wake_records_one_duplicate_without_changing_its_original_completion()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var photo = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
        await scenario.Images.Upsert(photo, ct);
        var accepted = await scenario.Writer.QueueGallery(tour.Id, [photo.Id], photo.Id, 1, 0, canPublish: false, ct);
        var intent = await scenario.Intents.GetIntent(tour.Id, accepted.Value, ct);
        var wake = await scenario.GetWake(intent.ShouldNotBeNull().Id, ct);
        var operation = new IdempotencyOperation(
            IdempotencyScope.From($"catalog.integration-event.{CatalogGalleryPlacementQueuedIntegrationEvent.EventType}.v{CatalogGalleryPlacementQueuedIntegrationEvent.EventVersion}"),
            IdempotencyKey.From(wake.EventId.ToString("N")));
        var claim = await scenario.StartInboxLease(operation, ct);
        var leasedRow = await scenario.GetInboxRowVersion(operation, ct);
        using var rootActivity = new Activity("test.catalog.inbox");
        rootActivity.SetIdFormat(ActivityIdFormat.W3C).Start();
        var outcomes = new ConcurrentQueue<string>();
        using var listener = new MeterListener
        {
            InstrumentPublished = static (instrument, meterListener) =>
            {
                if (string.Equals(instrument.Meter.Name, CatalogTelemetry.Name, StringComparison.Ordinal)
                    && string.Equals(instrument.Name, CatalogTelemetry.MetricIntegrationEvent, StringComparison.Ordinal))
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            if (Activity.Current?.TraceId != rootActivity.TraceId)
            {
                return;
            }

            foreach (var tag in tags)
            {
                if (string.Equals(tag.Key, CatalogTelemetry.TagOutcome, StringComparison.Ordinal) && tag.Value is string outcome)
                {
                    outcomes.Enqueue(outcome);
                }
            }
        });
        listener.Start();

        // Act
        Func<Task> deliverWithLiveLease = async () => await scenario.Handler.Handle(wake, ct);
        var busy = await deliverWithLiveLease.ShouldThrow<InvalidOperationException>();
        var stillLeasedRow = await scenario.GetInboxRowVersion(operation, ct);
        var duplicatesDuringLease = await scenario.ReadInboxDuplicates(wake.EventId, ct);
        var beforeCompletion = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(1), ct);

        scenario.AdvanceBeyondInboxLease();
        await scenario.Handler.Handle(wake, ct);
        var completedRow = await scenario.GetInboxRowVersion(operation, ct);
        var duplicatesAfterCompletion = await scenario.ReadInboxDuplicates(wake.EventId, ct);
        var committed = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(1), ct);

        await using var redelivery = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var observedAt = redelivery.AdvanceBeyondInboxLease();
        await redelivery.Handler.Handle(wake, ct);
        var afterDuplicate = await redelivery.GetInboxRowVersion(operation, ct);
        var duplicatesAfterRedelivery = await redelivery.ReadInboxDuplicates(wake.EventId, ct);
        var afterReplay = await redelivery.Events.Load(stream, afterRevision: StreamRevision.From(1), ct);

        // Assert
        claim.Started.ShouldBeTrue();
        busy.Message.ShouldContain("is already being processed.", StringComparison.Ordinal);
        leasedRow.ShouldStartWith("Started/", StringComparison.Ordinal);
        stillLeasedRow.ShouldBe(leasedRow);
        duplicatesDuringLease.Rows.ShouldBeEmpty();
        beforeCompletion.ShouldBeEmpty();
        completedRow.ShouldStartWith("Completed/", StringComparison.Ordinal);
        duplicatesAfterCompletion.Rows.ShouldBeEmpty();
        afterDuplicate.ShouldBe(completedRow);
        outcomes.ToArray().ShouldBe([CatalogTelemetry.OutcomeError, CatalogTelemetry.OutcomeSuccess, CatalogTelemetry.OutcomeSkipped]);
        var applied = committed.ShouldHaveSingleItem();
        applied.Data.ShouldBeOfType<CatalogTourGalleryChanged>().IntentId.ShouldBe(wake.EventId);
        afterReplay.ShouldHaveSingleItem().EventId.ShouldBe(applied.EventId);
        var duplicate = duplicatesAfterRedelivery.Rows.ShouldHaveSingleItem();
        duplicate.EventId.ShouldBe(wake.EventId);
        duplicate.DuplicateCount.ShouldBe(1);
        duplicate.LastObservedAt.ShouldBe(observedAt);
        duplicatesAfterRedelivery.Next.ShouldBeNull();
    }

    [Fact]
    public async Task An_unavailable_observation_does_not_block_a_completed_wake_before_the_inbox_upgrade()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var migrator = scenario.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261124085349_RecordCatalogGalleryAcceptanceTime", ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var photo = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
        await scenario.Images.Upsert(photo, ct);
        var accepted = await scenario.Writer.QueueGallery(tour.Id, [photo.Id], photo.Id, 1, 0, canPublish: false, ct);
        var intent = await scenario.Intents.GetIntent(tour.Id, accepted.Value, ct);
        var wake = await scenario.GetWake(intent.ShouldNotBeNull().Id, ct);
        var operation = new IdempotencyOperation(
            IdempotencyScope.From($"catalog.integration-event.{CatalogGalleryPlacementQueuedIntegrationEvent.EventType}.v{CatalogGalleryPlacementQueuedIntegrationEvent.EventVersion}"),
            IdempotencyKey.From(wake.EventId.ToString("N")));
        await scenario.Handler.Handle(wake, ct);
        var beforeUpgrade = await scenario.ProcessStatus.Read(tour.Id, accepted.Value, ct);
        var originalCompletion = await scenario.GetInboxRowVersion(operation, ct);
        var originalEvent = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(1), ct);
        using var rootActivity = new Activity("test.catalog.inbox.observation-failure");
        rootActivity.SetIdFormat(ActivityIdFormat.W3C).Start();
        var failureTagCounts = new ConcurrentQueue<int>();
        using var listener = new MeterListener
        {
            InstrumentPublished = static (instrument, meterListener) =>
            {
                if (string.Equals(instrument.Meter.Name, CatalogTelemetry.Name, StringComparison.Ordinal)
                    && string.Equals(instrument.Name, CatalogTelemetry.MetricIdempotencyOperation, StringComparison.Ordinal))
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            if (Activity.Current?.TraceId != rootActivity.TraceId)
            {
                return;
            }

            foreach (var tag in tags)
            {
                if (string.Equals(tag.Key, CatalogTelemetry.TagOutcome, StringComparison.Ordinal)
                    && tag.Value is string outcome && string.Equals(outcome, CatalogTelemetry.OutcomeDuplicateObservationFailed, StringComparison.Ordinal))
                {
                    failureTagCounts.Enqueue(tags.Length);
                }
            }
        });
        listener.Start();

        // Act
        await scenario.Handler.Handle(wake, ct);
        var completionBeforeUpgrade = await scenario.GetInboxRowVersion(operation, ct);
        await migrator.MigrateAsync(targetMigration: null, ct);
        await using var upgraded = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var beforeRedelivery = await upgraded.ReadInboxDuplicates(wake.EventId, ct);
        var observedAt = upgraded.AdvanceBeyondInboxLease();
        await upgraded.Handler.Handle(wake, ct);
        var afterRedelivery = await upgraded.ReadInboxDuplicates(wake.EventId, ct);
        var afterUpgrade = await upgraded.ProcessStatus.Read(tour.Id, accepted.Value, ct);
        var completionAfterUpgrade = await upgraded.GetInboxRowVersion(operation, ct);
        var eventAfterUpgrade = await upgraded.Events.Load(stream, afterRevision: StreamRevision.From(1), ct);

        // Assert
        var original = beforeUpgrade.ShouldNotBeNull();
        original.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        original.AcceptedAt.ShouldNotBeNull();
        completionBeforeUpgrade.ShouldBe(originalCompletion);
        failureTagCounts.ShouldHaveSingleItem().ShouldBe(1);
        afterUpgrade.ShouldBe(original);
        completionAfterUpgrade.ShouldBe(originalCompletion);
        eventAfterUpgrade.ShouldHaveSingleItem().EventId.ShouldBe(originalEvent.ShouldHaveSingleItem().EventId);
        beforeRedelivery.Rows.ShouldBeEmpty();
        var duplicate = afterRedelivery.Rows.ShouldHaveSingleItem();
        duplicate.EventId.ShouldBe(wake.EventId);
        duplicate.DuplicateCount.ShouldBe(1);
        duplicate.LastObservedAt.ShouldBe(observedAt);
    }
}
