using PandaBear.Testing;
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
        var record = DocumentAuditTrailMappings.Map(domainEvent, TimeProvider.System.GetUtcNow());

        // Assert
        record.ReasonCode.ShouldBe(expectedReasonCode);
    }

    [Theory]
    [InlineData(DocumentAuditOperation.Read)]
    [InlineData(DocumentAuditOperation.Download)]
    [InlineData(DocumentAuditOperation.PlaceHold)]
    [InlineData(DocumentAuditOperation.ReleaseHold)]
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
        Func<object?> map = () => DocumentAuditTrailMappings.Map(domainEvent, TimeProvider.System.GetUtcNow());

        // Assert
        _ = map.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Map_rejects_a_missing_lifecycle_event()
    {
        // Arrange
        Func<object?> map = () => DocumentAuditTrailMappings.Map(null!, TimeProvider.System.GetUtcNow());

        // Act
        var exception = map.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("domainEvent");
    }

    [Fact]
    public void Map_names_the_unsupported_operation_in_its_rejection()
    {
        // Arrange
        var domainEvent = new DocumentLifecycleAuditDomainEvent(
            "actor",
            "correlation",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            DocumentAuditOperation.Read);
        Func<object?> map = () => DocumentAuditTrailMappings.Map(domainEvent, TimeProvider.System.GetUtcNow());

        // Act
        var exception = map.ShouldThrow<ArgumentOutOfRangeException>();

        // Assert
        exception.ParamName.ShouldBe("operation");
        exception.Message.ShouldStartWith("Unsupported lifecycle audit operation.", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_rejects_a_lifecycle_event_whose_metadata_cannot_form_an_audit_record()
    {
        // Arrange
        var domainEvent = new DocumentLifecycleAuditDomainEvent(
            string.Empty,
            "correlation",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            DocumentAuditOperation.Generate);
        Func<object?> map = () => DocumentAuditTrailMappings.Map(domainEvent, TimeProvider.System.GetUtcNow());

        // Act
        var exception = map.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe("A successful document lifecycle event produced invalid audit metadata.");
    }
}
