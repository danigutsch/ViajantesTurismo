using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Test data for document command idempotency over fake stores.</summary>
internal static class DocumentIdempotencyTestData
{
    /// <summary>Creates idempotency over a store that starts new operations.</summary>
    internal static DocumentCommandIdempotency Create(FakeUnitOfWork unitOfWork) =>
        new(new CapturingNewIdempotencyStore(unitOfWork), unitOfWork, TimeProvider.System);

    /// <summary>Creates idempotency over a completed entry whose fingerprint combines the request fingerprint and document id.</summary>
    internal static DocumentCommandIdempotency CreateCompleted(
        IdempotencyScope scope,
        IdempotencyKey key,
        Guid documentId,
        string requestFingerprint,
        IUnitOfWork unitOfWork) => CreateCompletedWithFingerprint(
            scope,
            key,
            $"{requestFingerprint}:{documentId:N}",
            unitOfWork);

    /// <summary>Creates idempotency over a completed entry carrying the given result fingerprint.</summary>
    internal static DocumentCommandIdempotency CreateCompletedWithFingerprint(
        IdempotencyScope scope,
        IdempotencyKey key,
        string? resultFingerprint,
        IUnitOfWork unitOfWork)
    {
        var operation = new IdempotencyOperation(scope, key);
        var completedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
        var entry = new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Completed,
            completedAt.AddMinutes(-1),
            completedAt,
            resultFingerprint);
        return new DocumentCommandIdempotency(new ExistingIdempotencyStore(entry), unitOfWork, TimeProvider.System);
    }

    /// <summary>Creates idempotency over a started entry begun the given elapsed time before the fixed clock.</summary>
    internal static DocumentCommandIdempotency CreateStarted(
        IdempotencyScope scope,
        IdempotencyKey key,
        IUnitOfWork unitOfWork,
        TimeSpan? elapsed = null)
    {
        var now = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
        var startedAt = now - (elapsed ?? TimeSpan.Zero);
        var operation = new IdempotencyOperation(scope, key);
        var entry = new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Started,
            startedAt,
            null,
            null);
        return new DocumentCommandIdempotency(
            new ExistingIdempotencyStore(entry),
            unitOfWork,
            new FakeTimeProvider(now));
    }
}
