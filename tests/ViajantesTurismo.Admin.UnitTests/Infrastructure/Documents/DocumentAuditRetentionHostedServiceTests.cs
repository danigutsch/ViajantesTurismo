using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentAuditRetentionHostedServiceTests
{
    [Fact]
    public async Task RunBatch_purges_expired_audit_records_through_the_registered_handler()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
        await using var fixture = DocumentAuditRetentionHostedServiceFixture.Create(now, 2);

        // Act
        var removedCount = await fixture.RunBatch(CancellationToken.None);

        // Assert
        removedCount.ShouldBe(2);
        fixture.AuditStore.PurgeCalledAt.ShouldBe(now.UtcDateTime);
    }
}
