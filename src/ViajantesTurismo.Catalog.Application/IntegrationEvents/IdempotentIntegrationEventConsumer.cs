using System.Diagnostics;
using Microsoft.Extensions.Options;
using SharedKernel.Idempotency;
using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Application.IntegrationEvents;

/// <summary>
/// Applies inbox-style idempotency around Catalog integration event consumers.
/// </summary>
/// <typeparam name="TIntegrationEvent">The integration event type.</typeparam>
public sealed class IdempotentIntegrationEventConsumer<TIntegrationEvent>(
    IIntegrationEventHandler<TIntegrationEvent> inner,
    IIdempotencyStore idempotencyStore,
    IOptions<IntegrationEventOptions> options) : IIntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    private static readonly IdempotencyScope Scope = IdempotencyScope.From(
        $"catalog.integration-event.{TIntegrationEvent.EventType}.v{TIntegrationEvent.EventVersion}");

    /// <inheritdoc />
    public async ValueTask Handle(TIntegrationEvent notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        using var activity = CatalogTelemetry.ActivitySource.StartActivity(
            CatalogTelemetry.ActivityIntegrationEventHandle,
            ActivityKind.Consumer);
        activity?.SetTag(CatalogTelemetry.TagBoundedContext, "catalog");
        activity?.SetTag(CatalogTelemetry.TagIntegrationEventType, TIntegrationEvent.EventType);
        activity?.SetTag(CatalogTelemetry.TagIntegrationEventVersion, TIntegrationEvent.EventVersion);

        var operation = new IdempotencyOperation(Scope, IdempotencyKey.From(notification.EventId.ToString("N")));
        try
        {
            var startResult = await idempotencyStore.TryStart(
                operation,
                DateTimeOffset.UtcNow,
                options.Value.IdempotencyLockDuration,
                ct);
            if (!startResult.Started)
            {
                SetOutcome(activity, CatalogTelemetry.OutcomeSkipped);
                activity?.SetTag(CatalogTelemetry.TagIdempotencyOutcome, CatalogTelemetry.OutcomeSkipped);
                CatalogTelemetry.IdempotencyOperations.Add(1, CreateEventTags(CatalogTelemetry.OutcomeSkipped));
                CatalogTelemetry.IntegrationEvents.Add(1, CreateEventTags(CatalogTelemetry.OutcomeSkipped));

                return;
            }

            activity?.SetTag(CatalogTelemetry.TagIdempotencyOutcome, CatalogTelemetry.OutcomeAcquired);
            CatalogTelemetry.IdempotencyOperations.Add(1, CreateEventTags(CatalogTelemetry.OutcomeAcquired));

            await inner.Handle(notification, ct);
            await idempotencyStore.Complete(operation, DateTimeOffset.UtcNow, notification.EventId.ToString("N"), ct);

            SetOutcome(activity, CatalogTelemetry.OutcomeSuccess);
            activity?.SetTag(CatalogTelemetry.TagIdempotencyOutcome, CatalogTelemetry.OutcomeCompleted);
            CatalogTelemetry.IdempotencyOperations.Add(1, CreateEventTags(CatalogTelemetry.OutcomeCompleted));
            CatalogTelemetry.IntegrationEvents.Add(1, CreateEventTags(CatalogTelemetry.OutcomeSuccess));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetError(activity, ex);
            CatalogTelemetry.IdempotencyOperations.Add(1, CreateEventTags(CatalogTelemetry.OutcomeError));
            CatalogTelemetry.IntegrationEvents.Add(1, CreateEventTags(CatalogTelemetry.OutcomeError));

            throw;
        }
    }

    private static TagList CreateEventTags(string outcome)
    {
        return
        [
            new(CatalogTelemetry.TagIntegrationEventType, TIntegrationEvent.EventType),
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
