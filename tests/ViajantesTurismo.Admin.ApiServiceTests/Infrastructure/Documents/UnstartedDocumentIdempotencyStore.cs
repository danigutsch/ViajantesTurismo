using PandaBear.Idempotency;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

/// <summary>Idempotency store that records lookups and fails if an operation is started or completed.</summary>
internal sealed class UnstartedDocumentIdempotencyStore : IIdempotencyStore
{
    private readonly List<IdempotencyOperation> requestedOperations = [];

    /// <summary>Gets the operations looked up so far.</summary>
    public IReadOnlyList<IdempotencyOperation> RequestedOperations => requestedOperations;

    /// <summary>Records the operation and reports no entry.</summary>
    public ValueTask<IdempotencyEntry?> Get(IdempotencyOperation operation, CancellationToken ct)
    {
        requestedOperations.Add(operation);
        return ValueTask.FromResult<IdempotencyEntry?>(null);
    }

    /// <summary>Always throws because a missing document source must not start an operation.</summary>
    public ValueTask<IdempotencyStartResult> TryStart(IdempotencyOperation operation, DateTimeOffset startedAt, TimeSpan? lockDuration, CancellationToken ct) =>
        throw new InvalidOperationException("A missing document source must not start an idempotent operation.");

    /// <summary>Always throws because a missing document source must not complete an operation.</summary>
    public ValueTask Complete(IdempotencyOperation operation, DateTimeOffset completedAt, string? resultFingerprint, CancellationToken ct) =>
        throw new InvalidOperationException("A missing document source must not complete an idempotent operation.");

    /// <summary>Always throws because a missing document source must not stage a completion.</summary>
    public ValueTask StageCompletion(IdempotencyOperation operation, DateTimeOffset completedAt, string? resultFingerprint, CancellationToken ct) =>
        throw new InvalidOperationException("A missing document source must not stage an idempotent completion.");
}
