using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure.Documents;

/// <summary>Test data for the document audit trail sink.</summary>
internal static class DocumentAuditTrailSinkTestData
{
    private static readonly DateTime Now = new(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Creates an in-memory Admin write context scope.</summary>
    public static AdminWriteDbContextTestScope CreateContext() =>
        AdminWriteDbContextTestFactory.CreateWithDomainEventDispatcher(
            new OutboxEnqueuingDomainEventDispatcher(new FakeTimeProvider(new DateTimeOffset(Now))));

    /// <summary>Creates a valid audit record naming the given document.</summary>
    public static DocumentAuditRecord CreateRecord(Guid documentId)
    {
        var result = DocumentAuditRecord.Create(
            "actor",
            documentId,
            Guid.CreateVersion7(),
            1,
            DocumentAuditOperation.BeginReview,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualOperation,
            "correlation",
            Now);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
