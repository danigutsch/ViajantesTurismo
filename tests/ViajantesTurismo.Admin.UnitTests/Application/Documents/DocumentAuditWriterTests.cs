using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentAuditWriterTests
{
    [Fact]
    public async Task Add_rejects_a_missing_audit_context()
    {
        // Arrange
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var writer = new DocumentAuditWriter(auditStore, unitOfWork, TimeProvider.System);

        // Act
        var result = await writer.Add(
            null!,
            DocumentAuditOperation.Finalize,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualFinalize,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Add_persists_valid_metadata_in_the_current_unit_of_work()
    {
        // Arrange
        var occurredAt = new DateTimeOffset(2026, 7, 18, 10, 30, 0, TimeSpan.Zero);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var writer = new DocumentAuditWriter(auditStore, unitOfWork, new FakeTimeProvider(occurredAt));
        var auditContext = DocumentAuditTestData.CreateContext();
        var documentId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();

        // Act
        var result = await writer.Add(
            auditContext,
            DocumentAuditOperation.Read,
            documentId,
            bookingId,
            3,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var record = auditStore.Records.ShouldHaveSingleItem();
        record.ActorId.ShouldBe(auditContext.ActorId);
        record.CorrelationId.ShouldBe(auditContext.CorrelationId);
        record.DocumentId.ShouldBe(documentId);
        record.BookingId.ShouldBe(bookingId);
        record.DocumentRevision.ShouldBe(3);
        record.OccurredAtUtc.ShouldBe(occurredAt.UtcDateTime);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Add_does_not_persist_invalid_record_metadata()
    {
        // Arrange
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var writer = new DocumentAuditWriter(auditStore, unitOfWork, TimeProvider.System);

        // Act
        var result = await writer.Add(
            DocumentAuditTestData.CreateContext(),
            DocumentAuditOperation.Read,
            Guid.Empty,
            null,
            null,
            DocumentAuditOutcome.Rejected,
            DocumentAuditReasonCode.DocumentNotFound,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
