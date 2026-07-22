using SharedKernel.AuditTrail;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentLifecycleAuditEventTests
{
    [Fact]
    public void Create_emits_metadata_only_domain_event()
    {
        // Arrange
        var now = new DateTime(2026, 7, 17, 9, 0, 0, DateTimeKind.Utc);
        var auditContextResult = DocumentAuditContext.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            "9a3ca841b4354928861c660a6e4e1b99");
        var auditContext = auditContextResult.Value;

        // Act
        var result = DocumentLineage.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            DocumentLineageTestData.CreateContent(),
            now,
            auditContext);
        var lineage = result.Value;
        var document = lineage.Revisions.ShouldHaveSingleItem();
        var auditEvent = lineage.GetDomainEvents()
            .ShouldHaveSingleItem()
            .ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();

        // Assert
        auditContextResult.IsSuccess.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        auditEvent.ActorId.ShouldBe(auditContext.ActorId);
        auditEvent.CorrelationId.ShouldBe(auditContext.CorrelationId);
        auditEvent.DocumentId.ShouldBe(document.Id);
        auditEvent.BookingId.ShouldBe(document.BookingId);
        auditEvent.DocumentRevision.ShouldBe(document.Revision);
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.Generate);
        typeof(IAuditTrailEntry).IsAssignableFrom(typeof(DocumentAuditRecord)).ShouldBeTrue();
        var propertyNames = typeof(DocumentLifecycleAuditDomainEvent)
            .GetProperties()
            .Select(static property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        propertyNames.ShouldBe(
            ["ActorId", "BookingId", "CorrelationId", "DocumentId", "DocumentRevision", "Operation"]);
    }

    [Fact]
    public void Create_rejects_missing_audit_context()
    {
        // Arrange
        var now = new DateTime(2026, 7, 17, 9, 0, 0, DateTimeKind.Utc);

        // Act
        var result = DocumentLineage.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            DocumentLineageTestData.CreateContent(),
            now,
            null!);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ClearDomainEvents_removes_created_audit_evidence()
    {
        // Arrange
        var result = DocumentLineage.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            DocumentLineageTestData.CreateContent(),
            DateTime.UtcNow,
            DocumentAuditTestData.CreateContext());
        result.IsSuccess.ShouldBeTrue();
        var lineage = result.Value;

        // Act
        lineage.ClearDomainEvents();

        // Assert
        lineage.GetDomainEvents().ShouldBeEmpty();
    }
}
