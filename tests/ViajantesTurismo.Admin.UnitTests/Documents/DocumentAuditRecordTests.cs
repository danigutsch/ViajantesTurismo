using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentAuditRecordTests
{
    [Fact]
    public void Create_records_only_operation_metadata_with_a_24_month_retention()
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc);
        var actorId = "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4";
        var documentId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();

        // Act
        var result = DocumentAuditRecord.Create(
            actorId,
            documentId,
            bookingId,
            2,
            DocumentAuditOperation.Finalize,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualFinalize,
            "9a3ca841b4354928861c660a6e4e1b99",
            occurredAt);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var record = result.Value;
        record.ActorId.ShouldBe(actorId);
        record.DocumentId.ShouldBe(documentId);
        record.BookingId.ShouldBe(bookingId);
        record.DocumentRevision.ShouldBe(2);
        record.Operation.ShouldBe(DocumentAuditOperation.Finalize);
        record.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        record.ReasonCode.ShouldBe(DocumentAuditReasonCode.ManualFinalize);
        record.CorrelationId.ShouldBe("9a3ca841b4354928861c660a6e4e1b99");
        record.OccurredAtUtc.ShouldBe(occurredAt);
        record.RetentionExpiresAt.ShouldBe(occurredAt.AddMonths(DocumentAuditLimits.RetentionMonths));
    }

    [Fact]
    public void Create_allows_a_missing_document_rejection_without_booking_or_revision_metadata()
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc);
        var actorId = "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4";
        var documentId = Guid.CreateVersion7();

        // Act
        var result = DocumentAuditRecord.Create(
            actorId,
            documentId,
            null,
            null,
            DocumentAuditOperation.Read,
            DocumentAuditOutcome.Rejected,
            DocumentAuditReasonCode.DocumentNotFound,
            "9a3ca841b4354928861c660a6e4e1b99",
            occurredAt);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var record = result.Value;
        record.DocumentId.ShouldBe(documentId);
        record.BookingId.ShouldBeNull();
        record.DocumentRevision.ShouldBeNull();
    }
}
