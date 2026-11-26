using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

[TestFocus(TestFocus.CoreBehavior)]
[Trait(TestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentIdempotencyRetentionDrainTests
{
    [Fact]
    public async Task RunBatch_purges_expired_entries_through_the_generated_mediator()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
        var store = new FakeDocumentIdempotencyRetentionStore { PurgedCount = 5 };

        // Act
        var removedCount = await DocumentIdempotencyRetentionBatch.Run(now, store, TestContext.Current.CancellationToken);

        // Assert
        removedCount.ShouldBe(5);
        store.PurgeCutoff.ShouldBe(now - DocumentCommandIdempotency.ReplayWindow);
    }
}
