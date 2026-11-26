using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.EventSourcing;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Measures the point lookup of committed Gallery placement metadata.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryProcessStatusPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Reads_old_and_recent_placement_outcomes_without_scanning_the_tour_history()
    {
        // Arrange
        const int historyLength = 1_200;
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedPublishableDraft(ct);
        var retiringTour = CatalogTour.CreateDraft(Guid.CreateVersion7(), "coast", "Coast", Guid.CreateVersion7());
        var retiringStream = CatalogTourStreamIds.FromAdminTourId(retiringTour.AdminTourId);
        await scenario.Events.Append(retiringStream, ExpectedStreamRevision.NoStream, retiringTour.GetUncommittedEvents(), ct);
        await scenario.Runner.Project(ct);
        var retiringIntent = await scenario.Writer.QueueGallery(retiringTour.Id, [], null, 1, 0, canPublish: false, ct);
        var photo = PublicMediaImageTestFactory.CreateReadyImage(tour.Id, "first.jpg", "first.jpg", "sha256:first", "First", 0, true);
        await scenario.Images.Upsert(photo, ct);
        var stream = CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId);
        var initial = await scenario.Writer.GetGalleryEditorState(tour.Id, placementSequence: null, canPublish: false, ct);
        var first = await scenario.Writer.QueueGallery(tour.Id, [photo.Id], photo.Id, initial.Value.StreamVersion, 0, canPublish: false, ct);
        var firstIntent = await scenario.Intents.GetIntent(tour.Id, first.Value, ct);
        var firstWake = await scenario.GetWake(firstIntent.ShouldNotBeNull().Id, ct);
        await scenario.Writer.ApplyGalleryIntents(tour.Id, firstWake.Sequence, ct);
        var firstAppend = await scenario.Events.Load(stream, afterRevision: StreamRevision.From(initial.Value.StreamVersion), ct);
        var next = await scenario.Writer.GetGalleryEditorState(tour.Id, placementSequence: null, canPublish: false, ct);
        var second = await scenario.Writer.QueueGallery(tour.Id, [photo.Id], photo.Id, next.Value.StreamVersion, first.Value, canPublish: false, ct);
        var secondIntent = await scenario.Intents.GetIntent(tour.Id, second.Value, ct);
        var secondWake = await scenario.GetWake(secondIntent.ShouldNotBeNull().Id, ct);
        var published = await scenario.Writer.Publish(tour.Id, next.Value.StreamVersion, ct);
        published.IsSuccess.ShouldBeTrue();
        var publishedState = await scenario.Writer.GetGalleryEditorState(tour.Id, placementSequence: null, canPublish: false, ct);
        var presentations = Enumerable.Range(0, historyLength)
            .Select(number => new CatalogTourPresentationChanged(tour.Id, number % 2 == 0 ? "Andes north" : "Andes south",
                tour.Slug, "Ride the Andes.", "", "", "", ""))
            .ToArray();
        var publishedVersion = ExpectedStreamRevision.From(StreamRevision.From(publishedState.Value.StreamVersion));
        await scenario.Events.Append(stream, publishedVersion, presentations, ct);
        var relational = scenario.Database.Database;
        await relational.ExecuteSqlRawAsync(
            """ALTER TABLE "CatalogTourReadModels" ADD CONSTRAINT "RejectStatusProbeProjection" CHECK ("StreamVersion" < 1000)""", ct);
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        await CatalogGalleryProcessStatusIndexes.EnsureIndexes(dataSource, ct);
        await relational.ExecuteSqlRawAsync("""ANALYZE "event_sourcing".events""", ct);

        // Act
        var pending = await scenario.ProcessStatus.Read(tour.Id, second.Value, ct);
        var operationsPending = await scenario.GalleryOperations.Read(tour.Id, second.Value, ct);
        var pendingFiltered = await CatalogGalleryProcessStatusQueryPlan.CountFilteredEventRows(dataSource, tour.Id, tour.AdminTourId, second.Value, stream, ct);
        Func<Task> consume = async () => await scenario.Handler.Handle(secondWake, ct);
        await consume.ShouldThrow<CatalogTourProjectionPendingException>();
        scenario.Database.ChangeTracker.Clear();
        var rejectedAppend = await scenario.Events.Load(
            stream, afterRevision: StreamRevision.From(publishedState.Value.StreamVersion + historyLength), ct);
        var before = await scenario.ProcessStatus.Read(tour.Id, first.Value, ct);
        var after = await scenario.ProcessStatus.Read(tour.Id, second.Value, ct);
        var recentFiltered = await CatalogGalleryProcessStatusQueryPlan.CountFilteredEventRows(dataSource, tour.Id, tour.AdminTourId, second.Value, stream, ct);
        var missing = await scenario.ProcessStatus.Read(tour.Id, second.Value + 1, ct);
        var unknownTour = await scenario.ProcessStatus.Read(Guid.CreateVersion7(), first.Value, ct);
        var operations = await scenario.GalleryOperations.Read(tour.Id, first.Value, ct);
        var operationsMissing = await scenario.GalleryOperations.Read(tour.Id, second.Value + 1, ct);
        var editor = await scenario.Writer.GetGalleryEditorState(tour.Id, second.Value, canPublish: false, ct);
        await scenario.Retire(tour, ct);
        var afterRetirement = await scenario.ProcessStatus.Read(tour.Id, second.Value, ct);
        var operationsRejected = await scenario.GalleryOperations.Read(tour.Id, second.Value, ct);
        await scenario.Retire(retiringTour, ct);
        var retiredAppend = await scenario.Events.Load(retiringStream, afterRevision: StreamRevision.From(1), ct);
        var closed = await scenario.ProcessStatus.Read(retiringTour.Id, retiringIntent.Value, ct);
        var operationsOtherTour = await scenario.GalleryOperations.Read(retiringTour.Id, first.Value, ct);
        var operationsOtherMissing = await scenario.GalleryOperations.Read(retiringTour.Id, second.Value, ct);
        var marker = await scenario.Database.CatalogTourRetirements.AsNoTracking()
            .SingleAsync(row => row.AdminTourId == retiringTour.AdminTourId, ct);
        await relational.ExecuteSqlInterpolatedAsync($"""
            UPDATE "CatalogTourRetirements" SET "RetirementId" = {Guid.CreateVersion7()} WHERE "AdminTourId" = {retiringTour.AdminTourId}
            """, ct);
        await relational.ExecuteSqlInterpolatedAsync($"""
            UPDATE "CatalogTourRetirements" SET "RetirementId" = {marker.RetirementId} WHERE "AdminTourId" = {tour.AdminTourId}
            """, ct);
        var disjointRetirement = await scenario.GalleryOperations.Read(retiringTour.Id, first.Value, ct);

        // Assert
        var pendingState = pending.ShouldNotBeNull();
        pendingState.IntentId.ShouldBe(secondWake.EventId);
        pendingState.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Pending);
        pendingState.RecordedAt.ShouldBeNull();
        pendingState.TerminalEventId.ShouldBeNull();
        pendingState.CausationEventId.ShouldBeNull();
        operationsPending.ShouldNotBeNull().TerminalEventId.ShouldBeNull();
        operationsPending.CausationEventId.ShouldBeNull();
        var oldState = before.ShouldNotBeNull();
        oldState.IntentId.ShouldBe(firstWake.EventId);
        oldState.Status.ShouldBe(CatalogGalleryPlacementStatusDto.Applied);
        oldState.RecordedAt.ShouldNotBeNull();
        var appliedEvent = firstAppend.Where(envelope => envelope.Data is CatalogTourGalleryChanged change && change.IntentId == firstWake.EventId)
            .ShouldHaveSingleItem();
        oldState.TerminalEventId.ShouldBe(appliedEvent.EventId);
        oldState.CausationEventId.ShouldBeNull();
        appliedEvent.CausationEventId.ShouldBeNull();
        var newState = after.ShouldNotBeNull();
        newState.IntentId.ShouldBe(secondWake.EventId);
        newState.Status.ShouldBe(CatalogGalleryPlacementStatusDto.RejectedAfterPublication);
        newState.RecordedAt.ShouldNotBeNull();
        var rejectedEvent = rejectedAppend.ShouldHaveSingleItem();
        rejectedEvent.Data.ShouldBeOfType<CatalogTourGalleryIntentRejected>().IntentId.ShouldBe(secondWake.EventId);
        newState.TerminalEventId.ShouldBe(rejectedEvent.EventId);
        newState.CausationEventId.ShouldBe(secondWake.EventId);
        var retiredRejection = afterRetirement.ShouldNotBeNull();
        retiredRejection.Status.ShouldBe(newState.Status);
        retiredRejection.TerminalEventId.ShouldBe(rejectedEvent.EventId);
        retiredRejection.CausationEventId.ShouldBe(secondWake.EventId);
        operationsRejected.ShouldNotBeNull().TerminalEventId.ShouldBe(rejectedEvent.EventId);
        operationsRejected.CausationEventId.ShouldBe(secondWake.EventId);
        missing.ShouldBeNull();
        unknownTour.ShouldBeNull();
        var operationsPlacement = operations.ShouldNotBeNull();
        operationsPlacement.CatalogTourId.ShouldBe(tour.Id);
        operationsPlacement.Sequence.ShouldBe(first.Value);
        operationsPlacement.IntentId.ShouldBe(oldState.IntentId);
        operationsPlacement.Status.ShouldBe(oldState.Status);
        operationsPlacement.Revision.ShouldBe(oldState.Revision);
        operationsPlacement.AcceptedAt.ShouldBe(oldState.AcceptedAt);
        operationsPlacement.TerminalAt.ShouldBe(oldState.RecordedAt);
        operationsPlacement.TerminalEventId.ShouldBe(appliedEvent.EventId);
        operationsPlacement.CausationEventId.ShouldBeNull();
        operationsMissing.ShouldBeNull();
        var otherPlacement = operationsOtherTour.ShouldNotBeNull();
        otherPlacement.CatalogTourId.ShouldBe(retiringTour.Id);
        otherPlacement.Sequence.ShouldBe(operationsPlacement.Sequence);
        otherPlacement.IntentId.ShouldNotBe(operationsPlacement.IntentId);
        otherPlacement.Status.ShouldBe(CatalogGalleryPlacementStatusDto.ClosedByRetirement);
        operationsOtherMissing.ShouldBeNull();
        var closedState = closed.ShouldNotBeNull();
        closedState.Status.ShouldBe(CatalogGalleryPlacementStatusDto.ClosedByRetirement);
        closedState.IntentId.ShouldNotBe(oldState.IntentId);
        closedState.RecordedAt.ShouldNotBeNull();
        var retirement = retiredAppend.ShouldHaveSingleItem();
        retirement.Data.ShouldBeOfType<CatalogTourRetired>();
        var retirementCause = retirement.CausationEventId.ShouldNotBeNull();
        closedState.TerminalEventId.ShouldBe(retirement.EventId);
        closedState.CausationEventId.ShouldBe(retirementCause);
        otherPlacement.TerminalEventId.ShouldBe(retirement.EventId);
        otherPlacement.CausationEventId.ShouldBe(retirementCause);
        otherPlacement.TriggeringWakeEventId.ShouldBeNull();
        otherPlacement.TriggeringWake.ShouldBeNull();
        var retirementMetadata = JsonSerializer.SerializeToElement(otherPlacement, JsonSerializerOptions.Web).GetProperty("retirement");
        retirementMetadata.GetProperty("retirementId").GetGuid().ShouldBe(marker.RetirementId);
        retirementMetadata.GetProperty("withdrawnAt").GetDateTimeOffset().ShouldBe(marker.WithdrawnAt);
        var disjoint = disjointRetirement.ShouldNotBeNull();
        disjoint.Status.ShouldBe(CatalogGalleryPlacementStatusDto.ClosedByRetirement);
        JsonSerializer.SerializeToElement(disjoint, JsonSerializerOptions.Web).TryGetProperty("retirement", out _).ShouldBeFalse();
        disjoint.CausationEventId.ShouldBe(retirementCause);
        editor.Value.Outcome.ShouldNotBeNull().Status.ShouldBe(CatalogGalleryPlacementStatusDto.RejectedAfterPublication);
        editor.Value.ProjectedStreamVersion.ShouldBeLessThan(editor.Value.StreamVersion);
        var newRevision = newState.Revision.ShouldNotBeNull();
        newRevision.ShouldBe(editor.Value.StreamVersion);
        oldState.Revision.ShouldNotBeNull().ShouldBeLessThan(newRevision);

        pendingFiltered.ShouldBe(0);
        recentFiltered.ShouldBe(0);
    }
}
