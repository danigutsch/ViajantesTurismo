using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

internal sealed class EfBrandingProjectionStore(AdminWriteDbContext dbContext) : IBrandingProjectionStore
{
    public async Task<BrandingProjectionState?> GetCurrent(CancellationToken ct)
    {
        var record = await Load(ct).ConfigureAwait(false);
        return record?.ToState();
    }

    public Task<bool> WasProcessed(Guid eventId, CancellationToken ct) =>
        dbContext.Set<BrandingInboxRecord>().AnyAsync(message => message.EventId == eventId, ct);

    public void Replace(BrandingProjectionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var tracked = dbContext.Set<BrandingProjectionRecord>().Local
            .SingleOrDefault(record => record.Id == BrandingProjectionRecord.ProjectionId);
        if (tracked is null)
        {
            dbContext.Set<BrandingProjectionRecord>().Add(BrandingProjectionRecord.Create(state));
            return;
        }

        tracked.ReplaceWith(state);
    }

    public void Record(BrandingInboxEntry entry) => dbContext.Set<BrandingInboxRecord>().Add(BrandingInboxRecord.From(entry));

    // Projection replacement and inbox completion commit in one transaction. A concurrent worker that applied first
    // fails this save on the projection revision token or the inbox key; the transport then redelivers and the
    // inbox check makes the retry a no-op.
    public Task Save(CancellationToken ct) => dbContext.SaveChangesAsync(ct);

    private Task<BrandingProjectionRecord?> Load(CancellationToken ct) =>
        dbContext.Set<BrandingProjectionRecord>()
            .SingleOrDefaultAsync(record => record.Id == BrandingProjectionRecord.ProjectionId, ct);
}
