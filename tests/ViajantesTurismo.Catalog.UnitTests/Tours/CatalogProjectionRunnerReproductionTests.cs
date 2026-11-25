using System.Collections.Concurrent;
using System.Diagnostics;
using SharedKernel.EventSourcing;
using SharedKernel.Testing.Snapshots;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

/// <summary>Reproduces a Catalog read-model write failure and its retry with fresh synthetic state.</summary>
public sealed class CatalogProjectionRunnerReproductionTests
{
    private const string Scenario = "background.catalog-projection-retry";
    private const string DraftTitle = "Synthetic projection retry tour";
    private const string FailureMessage = "synthetic-catalog-projection-store-unavailable";
    private const long InitialPosition = 10;
    private const long EventPosition = 11;

    private static readonly DateTimeOffset RecordedAt = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly ReproductionOutcome Retried = new("reproduced", "checkpoint-advanced-after-retry",
        new Dictionary<string, string>(StringComparer.Ordinal) { ["catalog-projection"] = "caught-up" });

    [Theory]
    [InlineData("projection-retry-east")]
    [InlineData("projection-retry-west")]
    public async Task A_failed_projection_retries_from_its_checkpoint_and_leaves_no_events_to_replay(string slug)
    {
        // Arrange
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var cancellation = timeout.Token;
        var attemptId = Guid.NewGuid();
        var startedUtc = TimeProvider.System.GetUtcNow();
        var start = Stopwatch.GetTimestamp();
        var activities = new ConcurrentQueue<Activity>();
        using var listener = CatalogTelemetryTestsHelpers.CreateActivityListener(activities);
        using var attempt = new Activity(Scenario);
        attempt.SetIdFormat(ActivityIdFormat.W3C);
        attempt.SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        attempt.Start();
        using var scratch = new TemporaryDirectory();
        var directory = ReproductionFixtureEnvironment.GetLocalResultsDirectory() ?? scratch.Path;
        var revision = ReproductionFixtureEnvironment.GetSourceRevision(typeof(CatalogProjectionRunnerReproductionTests).Assembly);
        var specification = new ReproductionSpecification(Scenario, revision, [Retried]);
        var draftCreated = new CatalogTourDraftCreated(Guid.CreateVersion7(), Guid.CreateVersion7(), slug, DraftTitle, Guid.CreateVersion7(), slug);
        var eventStore = new CapturingEventStore();
        eventStore.AddReplayEvent(CatalogProjectionRunnerTestsHelpers.CreateEnvelope(EventPosition, draftCreated, RecordedAt));
        var readModelStore = new CapturingCatalogTourReadModelStore
        {
            BeforeDraftUpsert = static ct =>
            {
                ct.ThrowIfCancellationRequested();
                throw new InvalidOperationException(FailureMessage);
            }
        };
        var projection = new CatalogTourReadModelProjection(readModelStore);
        var initialCheckpoint = new ProjectionCheckpoint(projection.Name, InitialPosition);
        var checkpointStore = new CapturingProjectionCheckpointStore { CurrentCheckpoint = initialCheckpoint };
        var runner = new CatalogProjectionRunner(eventStore, checkpointStore, [projection]);

        // Act
        Func<Task> projectWithFailure = async () => await runner.Project(cancellation);
        var failed = await projectWithFailure.ShouldThrow<InvalidOperationException>();
        var failedStart = eventStore.LoadedAfterPosition;
        var failedCheckpoint = checkpointStore.CurrentCheckpoint;
        var failedSave = checkpointStore.SavedCheckpoint;
        var failedDraft = readModelStore.Draft;

        readModelStore.BeforeDraftUpsert = null;
        var replayed = await runner.Project(cancellation);
        var retryStart = eventStore.LoadedAfterPosition;
        var retryCheckpoint = checkpointStore.SavedCheckpoint;
        var exhausted = await runner.Project(cancellation);

        // Assert
        failed.Message.ShouldBe(FailureMessage);
        failedStart.ShouldBe(InitialPosition);
        failedCheckpoint.ShouldBe(initialCheckpoint);
        failedSave.ShouldBeNull();
        failedDraft.ShouldBeNull();

        retryStart.ShouldBe(InitialPosition);
        replayed.ShouldBe(1);
        var advancedCheckpoint = new ProjectionCheckpoint(projection.Name, EventPosition);
        retryCheckpoint.ShouldBe(advancedCheckpoint);
        checkpointStore.CurrentCheckpoint.ShouldBe(advancedCheckpoint);
        var draft = readModelStore.Drafts.ShouldHaveSingleItem();
        draft.CatalogTourId.ShouldBe(draftCreated.CatalogTourId);
        draft.Title.ShouldBe(DraftTitle);
        draft.Slug.ShouldBe(slug);
        draft.Position.ShouldBe(EventPosition);
        draft.UpdatedAt.ShouldBe(RecordedAt);
        exhausted.ShouldBe(0);
        eventStore.LoadedAfterPosition.ShouldBe(EventPosition);

        var spans = activities.Where(activity => activity.TraceId == attempt.TraceId && activity.ParentSpanId == attempt.SpanId
            && string.Equals(activity.OperationName, CatalogTelemetry.ActivityProjectionProcess, StringComparison.Ordinal)).ToArray();
        spans.ShouldMatchCollection(
            failedSpan => failedSpan.Status.ShouldBe(ActivityStatusCode.Error),
            retriedSpan => retriedSpan.Status.ShouldBe(ActivityStatusCode.Ok),
            skippedSpan => skippedSpan.Status.ShouldBe(ActivityStatusCode.Ok));
        var trace = spans.Select(static activity => activity.TraceId).Distinct().ShouldHaveSingleItem();
        trace.ShouldNotBe(default);
        var durationMs = (int)Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var manifest = new ReproductionManifest(revision, Scenario, attemptId, [trace], Retried, startedUtc, durationMs);
        var path = await ReproductionManifestCodec.Write(directory, specification, manifest, cancellation);
        var persisted = await ReproductionManifestCodec.Read(path, specification, cancellation);
        var json = await File.ReadAllTextAsync(path, cancellation);

        persisted.AttemptId.ShouldBe(attemptId);
        persisted.TraceIds.ShouldHaveSingleItem().ShouldBe(trace);
        persisted.Outcome.OutcomeCode.ShouldBe(Retried.OutcomeCode);
        json.ShouldNotContain(DraftTitle, StringComparison.Ordinal);
        json.ShouldNotContain(slug, StringComparison.Ordinal);
        json.ShouldNotContain(draftCreated.CatalogTourId.ToString(), StringComparison.Ordinal);
        json.ShouldNotContain(FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_manifest_rejects_raw_projection_error_text()
    {
        // Arrange
        using var scratch = new TemporaryDirectory();
        var revision = ReproductionFixtureEnvironment.GetSourceRevision(typeof(CatalogProjectionRunnerReproductionTests).Assembly);
        var specification = new ReproductionSpecification(Scenario, revision, [Retried]);
        var invalidOutcome = Retried with
        {
            OutcomeCode = FailureMessage
        };
        var input = new ReproductionManifest(revision, Scenario, Guid.NewGuid(), [],
            invalidOutcome, TimeProvider.System.GetUtcNow(), 0);
        var ct = TestContext.Current.CancellationToken;

        // Act
        Func<Task> writeError = async () => await ReproductionManifestCodec.Write(scratch.Path, specification, input, ct);

        // Assert
        await writeError.ShouldThrow<InvalidDataException>();
        Directory.EnumerateFiles(scratch.Path).ShouldBeEmpty();
    }
}
