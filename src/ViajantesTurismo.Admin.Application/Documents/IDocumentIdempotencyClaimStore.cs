using SharedKernel.Idempotency;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Commits a short-lived document processing claim separately from the document mutation.</summary>
public interface IDocumentIdempotencyClaimStore
{
    /// <summary>Attempts to claim an operation and records which lineage owns it for retention.</summary>
    /// <param name="operation">The scope and key of the operation.</param>
    /// <param name="startedAt">The time at which the claim begins.</param>
    /// <param name="lockDuration">The duration of exclusive processing ownership.</param>
    /// <param name="retentionOwner">The document lineage identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The new claim or the existing entry.</returns>
    ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation, DateTimeOffset startedAt, TimeSpan lockDuration, string retentionOwner, CancellationToken ct);
}
