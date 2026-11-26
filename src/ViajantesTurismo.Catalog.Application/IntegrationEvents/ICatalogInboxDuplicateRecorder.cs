using SharedKernel.Idempotency;

namespace ViajantesTurismo.Catalog.Application.IntegrationEvents;

/// <summary>Records completed duplicate deliveries from the Catalog inbox.</summary>
public interface ICatalogInboxDuplicateRecorder
{
    /// <summary>The prefix of Catalog integration event operations in the inbox.</summary>
    public const string ScopePrefix = "catalog.integration-event.";

    /// <summary>Records the duplicate while its original completed operation is retained.</summary>
    /// <param name="scope">The scope used by the original inbox operation.</param>
    /// <param name="eventId">The immutable integration event identifier.</param>
    /// <param name="observedAt">The time the completed delivery was observed.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that completes when the observation has been processed.</returns>
    Task Record(IdempotencyScope scope, Guid eventId, DateTimeOffset observedAt, CancellationToken ct);
}
