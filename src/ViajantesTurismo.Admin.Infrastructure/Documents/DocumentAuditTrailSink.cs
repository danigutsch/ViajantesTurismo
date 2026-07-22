using SharedKernel.AuditTrail;
using SharedKernel.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

internal sealed class DocumentAuditTrailSink : IAuditTrailSink<DocumentAuditRecord>
{
    public ValueTask Append(DocumentAuditRecord entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ct.ThrowIfCancellationRequested();

        if (CurrentSaveChangesDbContext.Current is not AdminWriteDbContext dbContext)
        {
            throw new InvalidOperationException("Document audit trail entries require the current AdminWriteDbContext SaveChanges operation.");
        }

        dbContext.DocumentAuditRecords.Add(entry);
        return ValueTask.CompletedTask;
    }
}
