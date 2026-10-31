using PandaBear.AuditTrail;
using PandaBear.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Appends document audit trail entries to the current <see cref="AdminWriteDbContext"/> save operation.</summary>
internal sealed class DocumentAuditTrailSink : IAuditTrailSink<DocumentAuditRecord>
{
    /// <summary>Stages the entry on the ambient write context, stamped with its document's lineage because the save has already passed the pre-save stamp; throws when no such context is saving.</summary>
    public async ValueTask Append(DocumentAuditRecord entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ct.ThrowIfCancellationRequested();

        if (CurrentSaveChangesDbContext.Current is not AdminWriteDbContext dbContext)
        {
            throw new InvalidOperationException("Document audit trail entries require the current AdminWriteDbContext SaveChanges operation.");
        }

        dbContext.DocumentAuditRecords.Add(entry);
        await DocumentAuditLineage.StampAddedRecords(dbContext, ct);
    }
}
