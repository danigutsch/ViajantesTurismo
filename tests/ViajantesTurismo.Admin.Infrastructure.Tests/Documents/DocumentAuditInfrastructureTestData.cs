using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

internal static class DocumentAuditInfrastructureTestData
{
    public static DocumentAuditRecord CreateRecord(DateTime occurredAtUtc)
    {
        var result = DocumentAuditRecord.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            DocumentAuditOperation.Finalize,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualFinalize,
            "9a3ca841b4354928861c660a6e4e1b99",
            occurredAtUtc);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
