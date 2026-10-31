using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>
/// The lineage an audit record belongs to, stored as a shadow column so the record still resolves to its lineage after
/// the revision it names was purged. Records with no document have none.
/// </summary>
internal static class DocumentAuditLineage
{
    /// <summary>The shadow property and column name.</summary>
    public const string PropertyName = "DocumentLineageId";

    /// <summary>
    /// The stamp for a record whose document did not exist when it was written, so no lineage and therefore no hold can
    /// ever cover it. A record with no document carries a null stamp; the database rejects a null stamp on a record
    /// that names a document.
    /// </summary>
    public static readonly Guid NoLineage = Guid.Empty;

    /// <summary>Stamps every newly added audit record that names a document with the lineage of that document.</summary>
    public static async Task StampAddedRecords(AdminWriteDbContext dbContext, CancellationToken ct)
    {
        var added = dbContext.ChangeTracker.Entries<DocumentAuditRecord>()
            .Where(entry => entry.State == EntityState.Added && entry.Entity.DocumentId is not null)
            .ToArray();
        if (added.Length == 0)
        {
            return;
        }

        var documentIds = added.Select(entry => entry.Entity.DocumentId!.Value).Distinct().ToArray();
        var lineageByDocument = dbContext.DocumentDrafts.Local
            .Where(draft => documentIds.Contains(draft.Id))
            .ToDictionary(draft => draft.Id, draft => draft.DocumentLineageId);
        var missing = documentIds.Where(id => !lineageByDocument.ContainsKey(id)).ToArray();
        if (missing.Length > 0)
        {
            var stored = await dbContext.DocumentDrafts
                .AsNoTracking()
                .Where(draft => missing.Contains(draft.Id))
                .Select(draft => new { draft.Id, draft.DocumentLineageId })
                .ToListAsync(ct);
            foreach (var draft in stored)
            {
                lineageByDocument[draft.Id] = draft.DocumentLineageId;
            }
        }

        foreach (var entry in added)
        {
            entry.Property(PropertyName).CurrentValue = lineageByDocument.GetValueOrDefault(entry.Entity.DocumentId!.Value, NoLineage);
        }
    }
}
