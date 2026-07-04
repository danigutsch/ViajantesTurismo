using SharedKernel.Idempotency;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class EfIdempotencyStoreTests
{
    [Fact]
    public async Task TryStart_creates_durable_started_entry()
    {
        await using var dbContext = CatalogDbContextTestFactory.Create();
        var store = new EfIdempotencyStore(dbContext);
        var operation = IdempotencyOperationTestFactory.Create();
        var startedAt = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

        var result = await store.TryStart(operation, startedAt, TimeSpan.FromMinutes(5), CancellationToken.None);

        result.Started.ShouldBeTrue();
        var entry = (await store.Get(operation, CancellationToken.None)).ShouldNotBeNull();
        entry.State.ShouldBe(IdempotencyEntryState.Started);
        entry.StartedAt.ShouldBe(startedAt);
    }

    [Fact]
    public async Task TryStart_treats_completed_entry_as_safe_duplicate()
    {
        await using var dbContext = CatalogDbContextTestFactory.Create();
        var store = new EfIdempotencyStore(dbContext);
        var operation = IdempotencyOperationTestFactory.Create();
        var startedAt = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        await store.TryStart(operation, startedAt, TimeSpan.FromMinutes(5), CancellationToken.None);
        await store.Complete(operation, startedAt.AddSeconds(10), resultFingerprint: null, CancellationToken.None);

        var duplicate = await store.TryStart(
            operation,
            startedAt.AddMinutes(10),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        duplicate.Started.ShouldBeFalse();
        duplicate.ExistingEntry.ShouldNotBeNull().State.ShouldBe(IdempotencyEntryState.Completed);
    }

    [Fact]
    public async Task TryStart_restarts_expired_started_entry()
    {
        await using var dbContext = CatalogDbContextTestFactory.Create();
        var store = new EfIdempotencyStore(dbContext);
        var operation = IdempotencyOperationTestFactory.Create();
        var startedAt = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var restartedAt = startedAt.AddMinutes(6);
        await store.TryStart(operation, startedAt, TimeSpan.FromMinutes(5), CancellationToken.None);

        var restarted = await store.TryStart(operation, restartedAt, TimeSpan.FromMinutes(5), CancellationToken.None);

        restarted.Started.ShouldBeTrue();
        var entry = (await store.Get(operation, CancellationToken.None)).ShouldNotBeNull();
        entry.State.ShouldBe(IdempotencyEntryState.Started);
        entry.StartedAt.ShouldBe(restartedAt);
    }
}
