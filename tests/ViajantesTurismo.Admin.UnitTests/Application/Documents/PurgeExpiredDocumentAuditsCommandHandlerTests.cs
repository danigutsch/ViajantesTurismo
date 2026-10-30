using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class PurgeExpiredDocumentAuditsCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_purges_expired_audit_records_using_the_current_utc_time()
    {
        // Arrange
        var auditStore = new FakeDocumentAuditStore { PurgedCount = 3 };
        var handler = DocumentAuditTestData.CreatePurgeHandler(auditStore, new FakeUnitOfWork(), Now);

        // Act
        var removedCount = await handler.Handle(DocumentAuditTestData.CreatePurgeCommand(), TestContext.Current.CancellationToken);

        // Assert
        removedCount.ShouldBe(3);
        auditStore.PurgeCalledAt.ShouldBe(Now.UtcDateTime);
    }

    [Fact]
    public async Task Handle_records_succeeded_purge_evidence_with_count_cutoff_actor_and_correlation()
    {
        // Arrange
        var auditStore = new FakeDocumentAuditStore { PurgedCount = 3 };
        var unitOfWork = new FakeUnitOfWork();
        var command = DocumentAuditTestData.CreatePurgeCommand();

        // Act
        await DocumentAuditTestData.CreatePurgeHandler(auditStore, unitOfWork, Now).Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var evidence = auditStore.PurgeRecords.ShouldHaveSingleItem();
        evidence.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        evidence.PurgedCount.ShouldBe(3);
        evidence.RetentionCutoffUtc.ShouldBe(Now.UtcDateTime);
        evidence.OccurredAtUtc.ShouldBe(Now.UtcDateTime);
        evidence.ActorId.ShouldBe(command.AuditContext.ActorId);
        evidence.CorrelationId.ShouldBe(command.AuditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_records_evidence_when_nothing_was_purged()
    {
        // Arrange
        var auditStore = new FakeDocumentAuditStore { PurgedCount = 0 };

        // Act
        await DocumentAuditTestData.CreatePurgeHandler(auditStore, new FakeUnitOfWork(), Now).Handle(DocumentAuditTestData.CreatePurgeCommand(), TestContext.Current.CancellationToken);

        // Assert
        auditStore.PurgeRecords.ShouldHaveSingleItem().PurgedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_records_rejected_evidence_and_rethrows_when_the_purge_fails()
    {
        // Arrange
        var failure = new InvalidOperationException("purge failed");
        var auditStore = new FakeDocumentAuditStore { PurgeFailure = failure };
        var unitOfWork = new FakeUnitOfWork();
        var handler = DocumentAuditTestData.CreatePurgeHandler(auditStore, unitOfWork, Now);

        // Act
        Func<Task> purge = () => handler.Handle(DocumentAuditTestData.CreatePurgeCommand(), TestContext.Current.CancellationToken);
        var thrown = await purge.ShouldThrow<InvalidOperationException>();

        // Assert
        thrown.ShouldBeSameAs(failure);
        var evidence = auditStore.PurgeRecords.ShouldHaveSingleItem();
        evidence.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        evidence.PurgedCount.ShouldBe(0);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
