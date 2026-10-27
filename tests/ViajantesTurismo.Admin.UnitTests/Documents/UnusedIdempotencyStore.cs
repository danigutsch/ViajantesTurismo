using PandaBear.Idempotency;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Idempotency store fake that fails any use, for tests that must not persist idempotency.</summary>
internal sealed class UnusedIdempotencyStore : IIdempotencyStore
{
    /// <summary>Always throws because the test did not expect idempotency persistence.</summary>
    public ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation,
        DateTimeOffset startedAt,
        TimeSpan? lockDuration,
        CancellationToken ct) => throw new InvalidOperationException("The test did not expect idempotency persistence.");

    /// <summary>Always throws because the test did not expect idempotency persistence.</summary>
    public ValueTask Complete(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException("The test did not expect idempotency persistence.");

    /// <summary>Always throws because the test did not expect idempotency persistence.</summary>
    public ValueTask StageCompletion(
        IdempotencyOperation operation,
        DateTimeOffset completedAt,
        string? resultFingerprint,
        CancellationToken ct) => throw new InvalidOperationException("The test did not expect idempotency persistence.");

    /// <summary>Always throws because the test did not expect idempotency persistence.</summary>
    public ValueTask<IdempotencyEntry?> Get(
        IdempotencyOperation operation,
        CancellationToken ct) => throw new InvalidOperationException("The test did not expect idempotency persistence.");
}
