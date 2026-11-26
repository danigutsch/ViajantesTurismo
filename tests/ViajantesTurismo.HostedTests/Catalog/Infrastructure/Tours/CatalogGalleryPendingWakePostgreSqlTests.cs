using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedKernel.EventSourcing;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies the bounded pending wake metadata for a concrete Gallery placement.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryPendingWakePostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Related_pending_wakes_are_scoped_to_the_placement_and_respect_delivery_retention()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var first = await scenario.SeedDraft(authoredGallery: false, ct);
        var second = await scenario.SeedDraft("other-tour", "Other tour", authoredGallery: false, ct);
        var (ordinaryId, sequence) = await scenario.QueuePhoto(first.Id, ct);
        var (otherId, otherSequence) = await scenario.QueuePhoto(second.Id, ct);
        otherSequence.ShouldBe(sequence);
        var initialized = await scenario.Writer.BootstrapLegacyGallery(first.Id, canPublish: false, ct);
        initialized.IsSuccess.ShouldBeTrue();
        var stream = CatalogTourStreamIds.FromAdminTourId(first.AdminTourId);
        var events = await scenario.Events.Load(stream, StreamRevision.From(1), ct);
        var bootstrap = events.ShouldHaveSingleItem();
        var bootstrapWakeId = bootstrap.EventId.ToString("D");
        var ordinaryWakeId = ordinaryId.ToString("D");
        var retiredWakeId = otherId.ToString("D");
        var lastAttempt = DateTimeOffset.UnixEpoch.AddDays(20);
        var nextAttempt = lastAttempt.AddSeconds(30);
        var laterAttempt = nextAttempt.AddSeconds(5);
        var staged = await scenario.Database.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE messaging.outbox_messages
            SET "PublishAttempts" = 2, "LastPublishAttemptAt" = {lastAttempt},
                "NextPublishAttemptAt" = CASE "EventId"
                    WHEN {bootstrapWakeId} THEN {nextAttempt}
                    WHEN {ordinaryWakeId} THEN {laterAttempt}
                    ELSE {nextAttempt.AddSeconds(-5)}
                END,
                "LastPublishError" = 'private synthetic error'
            WHERE "EventId" IN ({bootstrapWakeId}, {ordinaryWakeId}, {retiredWakeId})
            """, ct);
        staged.ShouldBe(3);
        await scenario.Retire(second, ct);

        // Act
        var beforeDue = await scenario.GalleryOperations.ReadRetryHealth(nextAttempt.AddSeconds(-1), ct);
        var onDeadline = await scenario.GalleryOperations.ReadRetryHealth(nextAttempt.ToOffset(TimeSpan.FromHours(1)), ct);
        var withTwoWakes = await scenario.GalleryOperations.ReadRetryHealth(laterAttempt, ct);
        var before = await scenario.GalleryOperations.Read(first.Id, sequence, ct);
        var unrelated = await scenario.GalleryOperations.Read(second.Id, otherSequence, ct);
        var missing = await scenario.GalleryOperations.Read(first.Id, sequence + 1, ct);
        await scenario.Handler.Handle(new CatalogGalleryPlacementQueuedIntegrationEvent(
            bootstrap.EventId, bootstrap.RecordedAt, first.Id, sequence), ct);
        var afterCommit = await scenario.GalleryOperations.ReadRetryHealth(laterAttempt, ct);
        var triggering = await scenario.GalleryOperations.Read(first.Id, sequence, ct);
        await scenario.Database.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM messaging.outbox_messages WHERE "EventId" = {bootstrapWakeId}
            """, ct);
        await scenario.TourProjection.Apply(bootstrap, ct);
        var afterRemoval = await scenario.GalleryOperations.Read(first.Id, sequence, ct);
        await scenario.Database.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE messaging.outbox_messages SET "PublishedAt" = {nextAttempt} WHERE "EventId" = {ordinaryWakeId}
            """, ct);
        var afterPublication = await scenario.GalleryOperations.Read(first.Id, sequence, ct);

        // Assert
        beforeDue.DuePlacementCount.ShouldBe(0L);
        beforeDue.OldestDueAt.ShouldBeNull();
        onDeadline.DuePlacementCount.ShouldBe(1L);
        onDeadline.OldestDueAt.ShouldBe(nextAttempt);
        withTwoWakes.ShouldBe(onDeadline);
        afterCommit.ShouldBe(beforeDue);
        var accepted = before.ShouldNotBeNull();
        accepted.OriginalWake.ShouldNotBeNull().PublishedAt.ShouldBeNull();
        var acceptedJson = JsonSerializer.SerializeToElement(accepted, JsonSerializerOptions.Web);
        var pending = acceptedJson.GetProperty("relatedPendingWakes").EnumerateArray().ToArray();
        var relatedIds = pending.Select(row => row.GetProperty("eventId").GetGuid()).Order().ToArray();
        Guid[] wakeIds = [ordinaryId, bootstrap.EventId];
        var orderedWakeIds = wakeIds.Order().ToArray();
        relatedIds.ShouldBe(orderedWakeIds);
        var retry = pending.Where(row => row.GetProperty("eventId").GetGuid() == bootstrap.EventId).ShouldHaveSingleItem();
        var retryFields = retry.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        retryFields.ShouldBe(["eventId", "failedAttempts", "lastAttemptAt", "nextAttemptAt", "recordedAt"]);
        retry.GetProperty("failedAttempts").GetInt32().ShouldBe(2);
        retry.GetProperty("lastAttemptAt").GetDateTimeOffset().ShouldBe(lastAttempt);
        retry.GetProperty("nextAttemptAt").GetDateTimeOffset().ShouldBe(nextAttempt);

        missing.ShouldBeNull();
        var unrelatedJson = JsonSerializer.SerializeToElement(unrelated.ShouldNotBeNull(), JsonSerializerOptions.Web);
        var otherWake = unrelatedJson.GetProperty("relatedPendingWakes").EnumerateArray().ToArray().ShouldHaveSingleItem();
        otherWake.GetProperty("eventId").GetGuid().ShouldBe(otherId);

        var terminal = afterRemoval.ShouldNotBeNull();
        terminal.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        var terminalJson = JsonSerializer.SerializeToElement(terminal, JsonSerializerOptions.Web);
        terminal.CausationEventId.ShouldBe(bootstrap.EventId);
        terminalJson.TryGetProperty("triggeringWake", out _).ShouldBeFalse();
        terminalJson.GetProperty("triggeringWakeEventId").GetGuid().ShouldBe(bootstrap.EventId);
        var triggeringJson = JsonSerializer.SerializeToElement(triggering.ShouldNotBeNull(), JsonSerializerOptions.Web);
        var retainedTrigger = triggeringJson.GetProperty("triggeringWake");
        retainedTrigger.GetProperty("failedAttempts").GetInt32().ShouldBe(2);
        retainedTrigger.GetProperty("nextAttemptAt").GetDateTimeOffset().ShouldBe(nextAttempt);
        var survivingWake = terminalJson.GetProperty("relatedPendingWakes").EnumerateArray().ToArray().ShouldHaveSingleItem();
        survivingWake.GetProperty("eventId").GetGuid().ShouldBe(ordinaryId);

        var published = afterPublication.ShouldNotBeNull();
        published.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        published.OriginalWake.ShouldNotBeNull().PublishedAt.ShouldBe(nextAttempt);
        var publishedJson = JsonSerializer.SerializeToElement(published, JsonSerializerOptions.Web);
        publishedJson.TryGetProperty("relatedPendingWakes", out _).ShouldBeFalse();
    }
}
