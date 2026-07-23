using SharedKernel.Idempotency;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal static class DocumentIdempotencyTestData
{
    internal static DocumentCommandIdempotency Create(IUnitOfWork unitOfWork) =>
        new(new UnusedIdempotencyStore(), unitOfWork, TimeProvider.System);

    internal static DocumentCommandIdempotency CreateCompleted(
        IdempotencyScope scope,
        IdempotencyKey key,
        Guid documentId,
        IUnitOfWork unitOfWork)
    {
        var operation = new IdempotencyOperation(scope, key);
        var completedAt = new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);
        var entry = new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Completed,
            completedAt.AddMinutes(-1),
            completedAt,
            documentId.ToString("N"));
        return new DocumentCommandIdempotency(new ExistingIdempotencyStore(entry), unitOfWork, TimeProvider.System);
    }

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
