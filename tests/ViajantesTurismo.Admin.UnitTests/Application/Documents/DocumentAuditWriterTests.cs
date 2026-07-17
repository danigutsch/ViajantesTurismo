using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentAuditWriterTests
{
    [Fact]
    public void Add_rejects_a_missing_audit_context()
    {
        // Arrange
        var auditStore = new FakeDocumentAuditStore();

        // Act
        var result = DocumentAuditWriter.Add(
            auditStore,
            null,
            DocumentAuditOperation.Finalize,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualFinalize,
            DateTime.UtcNow);

        // Assert
        result.IsFailure.ShouldBeTrue();
        auditStore.Records.ShouldBeEmpty();
    }
}
