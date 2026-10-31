using Microsoft.EntityFrameworkCore;
using PandaBear.DispositionHold;
using ViajantesTurismo.Admin.Application.Documents.Holds;

namespace ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

/// <summary>
/// Lists the document lineages that an effective hold restrains by the database clock. It lets a purge leave those
/// lineages out of its candidate query before the batch limit, so held rows cannot starve the batch. It is an
/// optimisation only: the single guard check inside the purge transaction stays the authoritative decision.
/// </summary>
internal static class ActiveHoldLineages
{
    /// <summary>Returns the lineage identifiers with a hold that is effective, not expired and not released now.</summary>
    public static async Task<Guid?[]> Find(AdminWriteDbContext dbContext, CancellationToken ct)
    {
        var databaseNow = await dbContext.Database.SqlQueryRaw<DateTime>("SELECT now() AS \"Value\"").SingleAsync(ct);
        var now = new DateTimeOffset(DateTime.SpecifyKind(databaseNow, DateTimeKind.Utc));
        var scopes = await dbContext.Set<Hold>()
            .AsNoTracking()
            .Where(hold => hold.ReleasedAt == null
                && hold.EffectiveFrom <= now
                && (hold.ExpiresAt == null || now < hold.ExpiresAt))
            .Select(hold => hold.Scope)
            .ToListAsync(ct);
        var lineages = new HashSet<Guid>();
        foreach (var scope in scopes)
        {
            if (DocumentHoldScopes.TryGetLineageId(scope, out var lineageId))
            {
                _ = lineages.Add(lineageId);
            }
        }

        return [.. lineages.Select(id => (Guid?)id)];
    }
}
