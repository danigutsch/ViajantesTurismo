using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class PurgeExpiredDocumentAuditsCommandHandlerTests
{
    [Fact]
    public async Task Handle_purges_expired_audit_records_using_the_current_utc_time()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
        var auditStore = new FakeDocumentAuditStore { PurgedCount = 3 };
        var handler = new PurgeExpiredDocumentAuditsCommandHandler(auditStore, new FakeTimeProvider(now));

        // Act
        var removedCount = await handler.Handle(new PurgeExpiredDocumentAuditsCommand(), CancellationToken.None);

        // Assert
        removedCount.ShouldBe(3);
        auditStore.PurgeCalledAt.ShouldBe(now.UtcDateTime);
    }
}
