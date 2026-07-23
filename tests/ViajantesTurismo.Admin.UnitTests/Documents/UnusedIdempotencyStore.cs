using SharedKernel.Idempotency;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class UnusedIdempotencyStore : IIdempotencyStore
{
    public ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration,
        CancellationToken ct) => throw new InvalidOperationException("The test did not expect idempotency persistence.");

    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException("The test did not expect idempotency persistence.");

    public ValueTask StageCompletion(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException("The test did not expect idempotency persistence.");

    public ValueTask<IdempotencyEntry?> Get(
        IdempotencyOperation operation,
        CancellationToken ct) => throw new InvalidOperationException("The test did not expect idempotency persistence.");
}
