using SharedKernel.Idempotency;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CapturingIdempotencyStore(bool started = true) : IIdempotencyStore
{
    public IdempotencyEntryState? CompletedState { get; private set; }

    public ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration,
        CancellationToken cancellationToken = default)
    {
        if (started)
        {
            return ValueTask.FromResult(IdempotencyStartResult.StartedNew());
        }

        return ValueTask.FromResult(IdempotencyStartResult.AlreadyStarted(new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Completed,
            startedAt,
            startedAt,
            ResultFingerprint: null)));
    }

    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint = null,
        CancellationToken cancellationToken = default)
    {
        CompletedState = IdempotencyEntryState.Completed;

        return ValueTask.CompletedTask;
    }

    public ValueTask<IdempotencyEntry?> Get(
        IdempotencyOperation operation,
        CancellationToken cancellationToken = default) => ValueTask.FromResult<IdempotencyEntry?>(null);
}
