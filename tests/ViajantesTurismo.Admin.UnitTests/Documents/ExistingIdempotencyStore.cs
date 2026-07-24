using SharedKernel.Idempotency;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class ExistingIdempotencyStore(IdempotencyEntry entry) : IIdempotencyStore
{
    public ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration,
        CancellationToken ct)
    {
        EnsureExpectedOperation(operation);
        return ValueTask.FromResult(IdempotencyStartResult.AlreadyStarted(entry));
    }

    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException("A completed replay cannot be completed again.");

    public ValueTask StageCompletion(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException("An existing operation cannot be completed again.");

    public ValueTask<IdempotencyEntry?> Get(
        IdempotencyOperation operation,
        CancellationToken ct)
    {
        EnsureExpectedOperation(operation);
        return ValueTask.FromResult<IdempotencyEntry?>(entry);
    }

    private void EnsureExpectedOperation(IdempotencyOperation operation)
    {
        if (operation != entry.Operation)
        {
            throw new InvalidOperationException("The idempotency operation did not match the configured entry.");
        }
    }
}
