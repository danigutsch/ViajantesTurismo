using PandaBear.BuildingBlocks;
using PandaBear.DispositionHold;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Purges document audit records only after their currently configured retention period and never those of a held
/// document lineage. The hold check and the deletion share one explicit transaction, and an unknown hold state
/// refuses the purge.
/// </summary>
public sealed class PurgeExpiredDocumentAuditsCommandHandler(
    IDocumentAuditStore auditStore,
    IUnitOfWork unitOfWork,
    IDocumentDispositionTransaction transaction,
    DocumentHoldGate holdGate,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Removes expired, unheld document audit records and records metadata-only evidence of the run in one save inside
    /// one transaction, and returns the purged and held counts. A failed run discards its staged changes and records
    /// rejected evidence; an unknown hold state records refused evidence and rethrows. Neither removes anything.
    /// </summary>
    public async Task<DocumentPurgeCounts> Handle(PurgeExpiredDocumentAuditsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.AuditContext);

        var cutoff = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            return await transaction.Execute([], () => PurgeAndRecordEvidence(command.AuditContext, cutoff, ct), ct);
        }
        catch (DispositionHoldStateUnknownException unknown)
        {
            await RecordFailureEvidence(command.AuditContext, cutoff, DocumentAuditOutcome.Refused, unknown);
            throw;
        }
        catch (Exception purgeFailure) when (purgeFailure.ShouldHandleAsFailure(ct))
        {
            await RecordFailureEvidence(command.AuditContext, cutoff, DocumentAuditOutcome.Rejected, purgeFailure);
            throw;
        }
    }

    private async Task<DocumentPurgeCounts> PurgeAndRecordEvidence(
        DocumentAuditContext auditContext,
        DateTime cutoff,
        CancellationToken ct)
    {
        var counts = await auditStore.PurgeExpiredRecords(cutoff, holdGate.FindHeldLineages, ct);
        await RecordEvidence(auditContext, counts, cutoff, DocumentAuditOutcome.Succeeded, ct);
        return counts;
    }

    private async Task RecordFailureEvidence(
        DocumentAuditContext auditContext,
        DateTime cutoff,
        DocumentAuditOutcome outcome,
        Exception purgeFailure)
    {
        auditStore.DiscardPendingChanges();
        try
        {
            await RecordEvidence(auditContext, default, cutoff, outcome, CancellationToken.None);
        }
        catch (Exception evidenceFailure)
        {
            throw new AggregateException(purgeFailure, evidenceFailure);
        }
    }

    private async Task RecordEvidence(
        DocumentAuditContext auditContext,
        DocumentPurgeCounts counts,
        DateTime cutoff,
        DocumentAuditOutcome outcome,
        CancellationToken ct)
    {
        var record = DocumentAuditPurgeRecord.Create(
            auditContext.ActorId,
            auditContext.CorrelationId,
            counts.Purged,
            counts.Held,
            cutoff,
            outcome,
            timeProvider.GetUtcNow().UtcDateTime);
        if (record.IsFailure)
        {
            throw new InvalidOperationException("Document audit purge evidence was invalid.");
        }

        auditStore.Add(record.Value);
        await unitOfWork.SaveEntities(ct);
    }
}
