using PandaBear.BuildingBlocks;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Purges document audit records only after their currently configured retention period.</summary>
public sealed class PurgeExpiredDocumentAuditsCommandHandler(
    IDocumentAuditStore auditStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Removes expired document audit records and records metadata-only evidence of the run in one save, and returns the count. A failed run discards its staged changes and records rejected evidence instead.</summary>
    public async Task<int> Handle(PurgeExpiredDocumentAuditsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.AuditContext);

        var cutoff = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            var purgedCount = await auditStore.PurgeExpiredRecords(cutoff, ct);
            await RecordEvidence(command.AuditContext, purgedCount, cutoff, DocumentAuditOutcome.Succeeded, ct);
            return purgedCount;
        }
        catch (Exception purgeFailure) when (purgeFailure.ShouldHandleAsFailure(ct))
        {
            auditStore.DiscardPendingChanges();
            try
            {
                await RecordEvidence(command.AuditContext, 0, cutoff, DocumentAuditOutcome.Rejected, CancellationToken.None);
            }
            catch (Exception evidenceFailure)
            {
                throw new AggregateException(purgeFailure, evidenceFailure);
            }

            throw;
        }
    }

    private async Task RecordEvidence(
        DocumentAuditContext auditContext,
        int purgedCount,
        DateTime cutoff,
        DocumentAuditOutcome outcome,
        CancellationToken ct)
    {
        var record = DocumentAuditPurgeRecord.Create(
            auditContext.ActorId,
            auditContext.CorrelationId,
            purgedCount,
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
