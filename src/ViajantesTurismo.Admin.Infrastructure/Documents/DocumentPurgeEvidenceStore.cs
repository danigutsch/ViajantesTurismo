using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Entity Framework persistence for counts-only draft and idempotency purge evidence.</summary>
internal sealed class DocumentPurgeEvidenceStore(AdminWriteDbContext dbContext) : IDocumentPurgeEvidenceStore
{
    /// <summary>Stages purge evidence for insertion on the next save.</summary>
    public void Add(DocumentRetentionPurgeRecord record) => dbContext.DocumentRetentionPurgeRecords.Add(record);

    /// <summary>Clears the change tracker so nothing staged and unsaved survives.</summary>
    public void DiscardPendingChanges() => dbContext.ChangeTracker.Clear();
}
