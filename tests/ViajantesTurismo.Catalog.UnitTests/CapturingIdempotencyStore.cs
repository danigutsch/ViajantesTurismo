using SharedKernel.Idempotency;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CapturingIdempotencyStore(
    bool started = true,
    IdempotencyEntryState existingState = IdempotencyEntryState.Completed) : IIdempotencyStore
{
    private readonly Dictionary<IdempotencyOperation, IdempotencyEntry> entries = [];

    public IdempotencyEntryState? CompletedState { get; private set; }

    public TimeSpan? CapturedLockDuration { get; private set; }

    public ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration,
        CancellationToken ct)
    {
        CapturedLockDuration = lockDuration;

        if (entries.TryGetValue(operation, out var existingEntry))
        {
            return ValueTask.FromResult(IdempotencyStartResult.AlreadyStarted(existingEntry));
        }

        if (!started)
        {
            var existing = new IdempotencyEntry(
                operation,
                existingState,
                startedAt,
                existingState is IdempotencyEntryState.Completed ? startedAt : null,
                ResultFingerprint: null);
            entries.Add(operation, existing);

            return ValueTask.FromResult(IdempotencyStartResult.AlreadyStarted(existing));
        }

        entries.Add(operation, new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Started,
            startedAt,
            startedAt,
            ResultFingerprint: null));

        return ValueTask.FromResult(IdempotencyStartResult.StartedNew());
    }

    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct)
    {
        CompletedState = IdempotencyEntryState.Completed;
        entries[operation] = new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Completed,
            completedAt,
            completedAt,
            resultFingerprint);

        return ValueTask.CompletedTask;
    }

    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        CancellationToken ct)
    {
        return Complete(operation, completedAt, resultFingerprint: null, ct);
    }

    public ValueTask<IdempotencyEntry?> Get(
        IdempotencyOperation operation,
        CancellationToken ct)
    {
        entries.TryGetValue(operation, out var entry);

        return ValueTask.FromResult(entry);
    }

    public void SimulateExpiredLease()
    {
        foreach (var operation in entries
                     .Where(static pair => pair.Value.State is IdempotencyEntryState.Started)
                     .Select(static pair => pair.Key)
                     .ToArray())
        {
            _ = entries.Remove(operation);
        }
    }
}
