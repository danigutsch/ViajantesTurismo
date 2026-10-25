using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

internal static class DocumentAuditInfrastructureTestData
{
    public static DocumentAuditRecord CreateRecord(
        DateTime occurredAtUtc,
        Guid? documentId = null,
        Guid? bookingId = null,
        int documentRevision = 1)
    {
        var result = DocumentAuditRecord.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            documentId ?? Guid.CreateVersion7(),
            bookingId ?? Guid.CreateVersion7(),
            documentRevision,
            DocumentAuditOperation.Finalize,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualFinalize,
            "9a3ca841b4354928861c660a6e4e1b99",
            occurredAtUtc);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
