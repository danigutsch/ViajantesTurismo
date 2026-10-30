using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class PurgeExpiredDocumentIdempotencyEntriesCommandHandlerTests
{
    [Fact]
    public async Task Handle_purges_entries_older_than_the_replay_window_and_returns_the_count()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
        var store = new FakeDocumentIdempotencyRetentionStore { PurgedCount = 5 };
        var handler = new PurgeExpiredDocumentIdempotencyEntriesCommandHandler(store, new FakeTimeProvider(now));

        // Act
        var removedCount = await handler.Handle(TestContext.Current.CancellationToken);

        // Assert
        removedCount.ShouldBe(5);
        store.PurgeCutoff.ShouldBe(now - DocumentCommandIdempotency.ReplayWindow);
    }
}
