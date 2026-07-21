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

    [Fact]
    public void Create_rejects_blank_actor_metadata()
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc);

        // Act
        var result = DocumentAuditRecord.Create(
            " ",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            DocumentAuditOperation.Read,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            "9a3ca841b4354928861c660a6e4e1b99",
            occurredAt);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_blank_correlation_metadata()
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc);

        // Act
        var result = DocumentAuditRecord.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            DocumentAuditOperation.Read,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            " ",
            occurredAt);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_a_non_utc_timestamp()
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Local);

        // Act
        var result = DocumentAuditRecord.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            DocumentAuditOperation.Read,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            "9a3ca841b4354928861c660a6e4e1b99",
            occurredAt);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_rejects_empty_resource_identifiers(bool emptyDocumentId)
    {
        // Arrange
        var documentId = emptyDocumentId ? Guid.Empty : Guid.CreateVersion7();
        var bookingId = emptyDocumentId ? Guid.CreateVersion7() : Guid.Empty;

        // Act
        var result = DocumentAuditRecord.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            documentId,
            bookingId,
            1,
            DocumentAuditOperation.Read,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            "9a3ca841b4354928861c660a6e4e1b99",
            new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc));

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_non_positive_document_revisions(int revision)
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc);

        // Act
        var result = DocumentAuditRecord.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            revision,
            DocumentAuditOperation.Read,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            "9a3ca841b4354928861c660a6e4e1b99",
            occurredAt);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_undefined_audit_classifications()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc);

        // Act
        var invalidOperation = DocumentAuditRecord.Create(
            "actor",
            documentId,
            bookingId,
            1,
            (DocumentAuditOperation)int.MaxValue,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            "correlation",
            occurredAt);
        var invalidOutcome = DocumentAuditRecord.Create(
            "actor",
            documentId,
            bookingId,
            1,
            DocumentAuditOperation.Read,
            (DocumentAuditOutcome)int.MaxValue,
            DocumentAuditReasonCode.None,
            "correlation",
            occurredAt);
        var invalidReason = DocumentAuditRecord.Create(
            "actor",
            documentId,
            bookingId,
            1,
            DocumentAuditOperation.Read,
            DocumentAuditOutcome.Succeeded,
            (DocumentAuditReasonCode)int.MaxValue,
            "correlation",
            occurredAt);

        // Assert
        invalidOperation.IsFailure.ShouldBeTrue();
        invalidOutcome.IsFailure.ShouldBeTrue();
        invalidReason.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DocumentAuditOperation.Finalize, DocumentAuditOutcome.Succeeded, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.Read, DocumentAuditOutcome.Succeeded, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.Generate, DocumentAuditOutcome.Rejected, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.Read, DocumentAuditOutcome.Rejected, DocumentAuditReasonCode.StateConflict)]
    public void Create_rejects_contradictory_outcome_reason_pairs(
        DocumentAuditOperation operation,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode)
    {
        // Arrange
        var documentId = operation == DocumentAuditOperation.Generate ? null : (Guid?)Guid.CreateVersion7();
        Guid? bookingId = operation == DocumentAuditOperation.Generate ? Guid.CreateVersion7() : null;

        // Act
        var result = DocumentAuditRecord.Create(
            "actor",
            documentId,
            bookingId,
            null,
            operation,
            outcome,
            reasonCode,
            "correlation",
            new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc));

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DocumentAuditOperation.Generate, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.Read, DocumentAuditReasonCode.None)]
    [InlineData(DocumentAuditOperation.BeginReview, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.RequestChanges, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.UpdateField, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.Approve, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.Finalize, DocumentAuditReasonCode.ManualFinalize)]
    [InlineData(DocumentAuditOperation.Regenerate, DocumentAuditReasonCode.ManualRegeneration)]
    [InlineData(DocumentAuditOperation.Void, DocumentAuditReasonCode.ManualVoid)]
    [InlineData(DocumentAuditOperation.Download, DocumentAuditReasonCode.None)]
    public void Create_accepts_supported_success_reason_codes(
        DocumentAuditOperation operation,
        DocumentAuditReasonCode reasonCode)
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc);

        // Act
        var result = DocumentAuditRecord.Create(
            "actor",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            operation,
            DocumentAuditOutcome.Succeeded,
            reasonCode,
            "correlation",
            occurredAt);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DocumentAuditOperation.Generate, DocumentAuditReasonCode.BookingNotFound)]
    [InlineData(DocumentAuditOperation.Generate, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.Generate, DocumentAuditReasonCode.ValidationRejected)]
    [InlineData(DocumentAuditOperation.Generate, DocumentAuditReasonCode.TourNotFound)]
    [InlineData(DocumentAuditOperation.Read, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.BeginReview, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.BeginReview, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.RequestChanges, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.RequestChanges, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.UpdateField, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.UpdateField, DocumentAuditReasonCode.ValidationRejected)]
    [InlineData(DocumentAuditOperation.UpdateField, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.Approve, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.Approve, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.Finalize, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.Finalize, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.Regenerate, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.Regenerate, DocumentAuditReasonCode.BookingNotFound)]
    [InlineData(DocumentAuditOperation.Regenerate, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.Regenerate, DocumentAuditReasonCode.ValidationRejected)]
    [InlineData(DocumentAuditOperation.Regenerate, DocumentAuditReasonCode.TourNotFound)]
    [InlineData(DocumentAuditOperation.Void, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.Void, DocumentAuditReasonCode.StateConflict)]
    [InlineData(DocumentAuditOperation.Void, DocumentAuditReasonCode.ValidationRejected)]
    [InlineData(DocumentAuditOperation.Download, DocumentAuditReasonCode.DocumentNotFound)]
    [InlineData(DocumentAuditOperation.Download, DocumentAuditReasonCode.ArtifactUnavailable)]
    public void Create_accepts_supported_rejection_reason_codes(
        DocumentAuditOperation operation,
        DocumentAuditReasonCode reasonCode)
    {
        // Arrange
        var documentId = operation == DocumentAuditOperation.Generate ? null : (Guid?)Guid.CreateVersion7();
        Guid? bookingId = operation == DocumentAuditOperation.Generate ? Guid.CreateVersion7() : null;

        // Act
        var result = DocumentAuditRecord.Create(
            "actor",
            documentId,
            bookingId,
            null,
            operation,
            DocumentAuditOutcome.Rejected,
            reasonCode,
            "correlation",
            new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc));

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Create_rejects_success_with_incomplete_resource_metadata(
        bool omitDocument,
        bool omitBooking,
        bool omitRevision)
    {
        // Arrange
        var documentId = omitDocument ? null : (Guid?)Guid.CreateVersion7();
        var bookingId = omitBooking ? null : (Guid?)Guid.CreateVersion7();
        int? revision = omitRevision ? null : 1;

        // Act
        var result = DocumentAuditRecord.Create(
            "actor",
            documentId,
            bookingId,
            revision,
            DocumentAuditOperation.Read,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            "correlation",
            new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc));

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_revision_without_complete_resource_identifiers()
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc);

        // Act
        var result = DocumentAuditRecord.Create(
            "actor",
            Guid.CreateVersion7(),
            null,
            1,
            DocumentAuditOperation.Read,
            DocumentAuditOutcome.Rejected,
            DocumentAuditReasonCode.DocumentNotFound,
            "correlation",
            occurredAt);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_rejects_rejection_without_a_requested_resource()
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 8, 30, 0, DateTimeKind.Utc);

        // Act
        var result = DocumentAuditRecord.Create(
            "actor",
            null,
            null,
            null,
            DocumentAuditOperation.Generate,
            DocumentAuditOutcome.Rejected,
            DocumentAuditReasonCode.BookingNotFound,
            "correlation",
            occurredAt);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }
}
