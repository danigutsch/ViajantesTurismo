using SharedKernel.AuditTrail;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Mappings;

internal static class DocumentAuditTrailMappings
{
    [AuditTrailMapping]
    public static DocumentAuditRecord Map(DocumentLifecycleAuditDomainEvent domainEvent, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var recordResult = DocumentAuditRecord.Create(
            domainEvent.ActorId,
            domainEvent.DocumentId,
            domainEvent.BookingId,
            domainEvent.DocumentRevision,
            domainEvent.Operation,
            DocumentAuditOutcome.Succeeded,
            GetSuccessReasonCode(domainEvent.Operation),
            domainEvent.CorrelationId,
            occurredAt.UtcDateTime);
        if (recordResult.IsFailure)
        {
            throw new InvalidOperationException("A successful document lifecycle event produced invalid audit metadata.");
        }

        return recordResult.Value;
    }

    private static DocumentAuditReasonCode GetSuccessReasonCode(DocumentAuditOperation operation) => operation switch
    {
        DocumentAuditOperation.Finalize => DocumentAuditReasonCode.ManualFinalize,
        DocumentAuditOperation.Regenerate => DocumentAuditReasonCode.ManualRegeneration,
        DocumentAuditOperation.Void => DocumentAuditReasonCode.ManualVoid,
        DocumentAuditOperation.Generate or
            DocumentAuditOperation.BeginReview or
            DocumentAuditOperation.RequestChanges or
            DocumentAuditOperation.UpdateField or
            DocumentAuditOperation.Approve => DocumentAuditReasonCode.ManualOperation,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported lifecycle audit operation."),
    };
}
