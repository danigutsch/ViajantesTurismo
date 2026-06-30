using System.Diagnostics;
using SharedKernel.BuildingBlocks;
using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.Application.Projections;

/// <summary>
/// Replays event-store events into Catalog projections and persists projection checkpoints.
/// </summary>
public sealed class CatalogProjectionRunner(
    IEventStore eventStore,
    IProjectionCheckpointStore checkpointStore,
    IEnumerable<IProjection> projections)
{
    private const int BatchSize = 100;

    /// <summary>
    /// Projects one batch of events for each configured projection.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    public async ValueTask Project(CancellationToken ct)
    {
        foreach (var projection in projections)
        {
            using var activity = CatalogTelemetry.ActivitySource.StartActivity(
                CatalogTelemetry.ActivityProjectionProcess,
                ActivityKind.Internal);
            activity?.SetTag(CatalogTelemetry.TagBoundedContext, "catalog");
            activity?.SetTag(CatalogTelemetry.TagProjectionName, projection.Name);

            try
            {
                var checkpoint = await checkpointStore.GetCheckpoint(projection.Name, ct);
                var position = checkpoint?.Position ?? 0;
                activity?.SetTag(CatalogTelemetry.TagCheckpointPosition, position);

                var envelopes = await eventStore.LoadAfter(position, BatchSize, ct);
                activity?.SetTag(CatalogTelemetry.TagEventCount, envelopes.Count);
                if (envelopes.Count == 0)
                {
                    SetOutcome(activity, CatalogTelemetry.OutcomeSkipped);
                    CatalogTelemetry.ProjectionBatches.Add(1, CreateProjectionTags(projection.Name, CatalogTelemetry.OutcomeSkipped));

                    continue;
                }

                var lastPosition = position;
                foreach (var envelope in envelopes.OrderBy(static envelope => envelope.Position))
                {
                    await projection.Apply(envelope, ct);
                    lastPosition = envelope.Position;
                }

                await checkpointStore.Save(new ProjectionCheckpoint(projection.Name, lastPosition), ct);

                activity?.SetTag(CatalogTelemetry.TagCheckpointPosition, lastPosition);
                SetOutcome(activity, CatalogTelemetry.OutcomeSuccess);
                CatalogTelemetry.ProjectionEvents.Add(envelopes.Count, CreateProjectionTags(projection.Name, CatalogTelemetry.OutcomeSuccess));
                CatalogTelemetry.ProjectionBatches.Add(1, CreateProjectionTags(projection.Name, CatalogTelemetry.OutcomeSuccess));
            }
            catch (OperationCanceledException ex)
            {
                if (!ex.ShouldHandleAsFailure(ct))
                {
                    throw;
                }

                SetError(activity, ex);
                CatalogTelemetry.ProjectionBatches.Add(1, CreateProjectionTags(projection.Name, CatalogTelemetry.OutcomeError));

                throw;
            }
            catch (Exception ex)
            {
                SetError(activity, ex);
                CatalogTelemetry.ProjectionBatches.Add(1, CreateProjectionTags(projection.Name, CatalogTelemetry.OutcomeError));

                throw;
            }
        }
    }

    private static TagList CreateProjectionTags(string projectionName, string outcome)
    {
        return
        [
            new(CatalogTelemetry.TagProjectionName, projectionName),
            new(CatalogTelemetry.TagOutcome, outcome),
        ];
    }

    private static void SetOutcome(Activity? activity, string outcome)
    {
        activity?.SetTag(CatalogTelemetry.TagOutcome, outcome);
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static void SetError(Activity? activity, Exception exception)
    {
        activity?.SetTag(CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError);
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.AddException(exception);
    }
}
