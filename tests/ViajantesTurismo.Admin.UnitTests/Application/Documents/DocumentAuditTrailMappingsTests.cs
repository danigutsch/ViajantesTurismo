using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentAuditTrailMappingsTests
{
    [Theory]
    [InlineData(DocumentAuditOperation.Generate, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.BeginReview, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.RequestChanges, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.UpdateField, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.Approve, DocumentAuditReasonCode.ManualOperation)]
    [InlineData(DocumentAuditOperation.Finalize, DocumentAuditReasonCode.ManualFinalize)]
    [InlineData(DocumentAuditOperation.Regenerate, DocumentAuditReasonCode.ManualRegeneration)]
    [InlineData(DocumentAuditOperation.Void, DocumentAuditReasonCode.ManualVoid)]
    public void Map_uses_the_supported_lifecycle_reason_code(
        DocumentAuditOperation operation,
        DocumentAuditReasonCode expectedReasonCode)
    {
        // Arrange
        var domainEvent = new DocumentLifecycleAuditDomainEvent(
            "actor",
            "correlation",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            operation);

        // Act
        var record = DocumentAuditTrailMappings.Map(domainEvent, DateTimeOffset.UtcNow);

        // Assert
        record.ReasonCode.ShouldBe(expectedReasonCode);
    }

    [Theory]
    [InlineData(DocumentAuditOperation.Read)]
    [InlineData(DocumentAuditOperation.Download)]
    [InlineData((DocumentAuditOperation)int.MaxValue)]
    public void Map_rejects_non_lifecycle_operations(DocumentAuditOperation operation)
    {
        // Arrange
        var domainEvent = new DocumentLifecycleAuditDomainEvent(
            "actor",
            "correlation",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            operation);

        // Act
        Func<object?> map = () => DocumentAuditTrailMappings.Map(domainEvent, DateTimeOffset.UtcNow);

        // Assert
        _ = map.ShouldThrow<ArgumentOutOfRangeException>();
    }
}
