using SharedKernel.Idempotency;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class UnexpectedCancelledIdempotencyStore : IIdempotencyStore
{
    public ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration,
        CancellationToken ct) => throw new OperationCanceledException(ct);

    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => ValueTask.CompletedTask;

    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        CancellationToken ct) => Complete(operation, completedAt, resultFingerprint: null, ct);

    public ValueTask<IdempotencyEntry?> Get(
        IdempotencyOperation operation,
        CancellationToken ct) => ValueTask.FromResult<IdempotencyEntry?>(null);
}
