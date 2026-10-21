using PandaBear.Idempotency;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Documents;

internal sealed class UnstartedDocumentIdempotencyStore : IIdempotencyStore
{
    private readonly List<IdempotencyOperation> requestedOperations = [];

    public IReadOnlyList<IdempotencyOperation> RequestedOperations => requestedOperations;

    public ValueTask<IdempotencyEntry?> Get(IdempotencyOperation operation, CancellationToken ct)
    {
        requestedOperations.Add(operation);
        return ValueTask.FromResult<IdempotencyEntry?>(null);
    }

    public ValueTask<IdempotencyStartResult> TryStart(IdempotencyOperation operation, DateTimeOffset startedAt, TimeSpan? lockDuration, CancellationToken ct) =>
        throw new InvalidOperationException("A missing document source must not start an idempotent operation.");

    public ValueTask Complete(IdempotencyOperation operation, DateTimeOffset completedAt, string? resultFingerprint, CancellationToken ct) =>
        throw new InvalidOperationException("A missing document source must not complete an idempotent operation.");

    public ValueTask StageCompletion(IdempotencyOperation operation, DateTimeOffset completedAt, string? resultFingerprint, CancellationToken ct) =>
        throw new InvalidOperationException("A missing document source must not stage an idempotent completion.");
}
