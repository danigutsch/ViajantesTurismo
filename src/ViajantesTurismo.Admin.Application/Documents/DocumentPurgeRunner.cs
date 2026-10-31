using PandaBear.DispositionHold;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Runs a retention purge and its counts-only evidence in one transaction. A run whose hold state is unknown removes
/// nothing and records refused evidence in a fresh save before the failure is rethrown.
/// </summary>
public sealed class DocumentPurgeRunner(
    IDocumentDispositionTransaction transaction,
    IDocumentPurgeEvidenceStore evidenceStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Runs <paramref name="purge" /> and saves succeeded evidence inside one transaction and returns the counts.
    /// An unknown hold state records refused evidence and rethrows.
    /// </summary>
    public async Task<DocumentPurgeCounts> Run(
        DocumentRetentionPurgeKind kind,
        Func<Task<DocumentPurgeCounts>> purge,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(purge);

        try
        {
            return await transaction.Execute([], () => PurgeAndRecordEvidence(kind, purge, ct), ct);
        }
        catch (DispositionHoldStateUnknownException unknown)
        {
            await RecordRefusedEvidence(kind, unknown);
            throw;
        }
    }

    private async Task<DocumentPurgeCounts> PurgeAndRecordEvidence(
        DocumentRetentionPurgeKind kind,
        Func<Task<DocumentPurgeCounts>> purge,
        CancellationToken ct)
    {
        var counts = await purge();
        await RecordEvidence(kind, counts, DocumentRetentionPurgeOutcome.Succeeded, ct);
        return counts;
    }

    private async Task RecordRefusedEvidence(DocumentRetentionPurgeKind kind, Exception refusal)
    {
        evidenceStore.DiscardPendingChanges();
        try
        {
            await RecordEvidence(kind, default, DocumentRetentionPurgeOutcome.Refused, CancellationToken.None);
        }
        catch (Exception evidenceFailure)
        {
            throw new AggregateException(refusal, evidenceFailure);
        }
    }

    private async Task RecordEvidence(
        DocumentRetentionPurgeKind kind,
        DocumentPurgeCounts counts,
        DocumentRetentionPurgeOutcome outcome,
        CancellationToken ct)
    {
        var record = DocumentRetentionPurgeRecord.Create(
            kind, counts.Purged, counts.Held, outcome, timeProvider.GetUtcNow().UtcDateTime);
        if (record.IsFailure)
        {
            throw new InvalidOperationException("Document retention purge evidence was invalid.");
        }

        evidenceStore.Add(record.Value);
        await unitOfWork.SaveEntities(ct);
    }
}
