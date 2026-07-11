using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentDraftRetentionHostedServiceTests
{
    [Fact]
    public async Task RunBatch_purges_expired_drafts_through_the_registered_handler()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero);
        var expired = DocumentDraftTestData.Create(now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        await using var fixture = DocumentDraftRetentionHostedServiceFixture.Create(now, expired);

        // Act
        var removedCount = await fixture.RunBatch(CancellationToken.None);

        // Assert
        removedCount.ShouldBe(1);
        fixture.DocumentStore.Documents.ContainsKey(expired.Id).ShouldBeFalse();
    }
}
