using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PandaBear.Branding;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Branding.Infrastructure;

internal sealed class EfBrandingSettingsStore(
    BrandingDbContext dbContext,
    [FromKeyedServices(typeof(BrandingDbContext))] IIntegrationEventOutbox outbox,
    TimeProvider timeProvider) : IBrandingSettingsStore, IBrandingSettingsRevisionStore, IBrandingSourceRecovery
{
    public async Task<BrandingSettings?> GetSettings(CancellationToken ct)
    {
        var entity = await Load(ct).ConfigureAwait(false);

        return entity?.ToSettings(BrandingFontFamilies.All);
    }

    public async Task<BrandingSettingsSnapshot?> GetCurrent(CancellationToken ct) =>
        ToSnapshot(await Load(ct).ConfigureAwait(false));

    public async Task<BrandingSettingsSnapshot?> SaveIfMatch(
        BrandingSettings settings,
        Func<BrandingSettingsSnapshot?, bool> precondition,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(precondition);

        var existing = await Load(ct).ConfigureAwait(false);
        if (!precondition(ToSnapshot(existing)))
        {
            return null;
        }

        await SaveSettings(settings, ct).ConfigureAwait(false);
        return ToSnapshot(await Load(ct).ConfigureAwait(false));
    }

    /// <summary>
    /// Saves a new revision: the current row, its immutable history entry, and the state-transfer event
    /// commit together. A writer holding a stale revision fails with <see cref="DbUpdateConcurrencyException" />.
    /// </summary>
    public async Task SaveSettings(BrandingSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var existing = await Load(ct).ConfigureAwait(false);
        if (existing is null)
        {
            existing = BrandingSettingsRecord.StartLineage(settings, Guid.CreateVersion7());
            dbContext.BrandingSettings.Add(existing);
        }
        else if (existing.HasSameValues(settings))
        {
            return;
        }
        else
        {
            existing.ApplyRevision(settings);
        }

        await CommitRevision(existing, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts revisions once: an existing valid row becomes revision 1, a missing row becomes the defaults
    /// at revision 1, and invalid persisted settings fail without writing anything.
    /// </summary>
    public async Task Bootstrap(CancellationToken ct)
    {
        var existing = await Load(ct).ConfigureAwait(false);
        if (existing is { IsRevisioned: true })
        {
            return;
        }

        if (existing is null)
        {
            existing = BrandingSettingsRecord.StartLineage(BrandingDefaults.CreateSettings(), Guid.CreateVersion7());
            dbContext.BrandingSettings.Add(existing);
        }
        else
        {
            if (existing.ToSettings(BrandingFontFamilies.All) is null)
            {
                throw new InvalidOperationException(
                    "Persisted Branding settings are invalid; correct them before Branding revisions can start.");
            }

            existing.StartRevisions(Guid.CreateVersion7());
        }

        try
        {
            await CommitRevision(existing, ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another bootstrapper started revisions first; that single revision 1 stands.
            dbContext.ChangeTracker.Clear();
            if (await Load(ct).ConfigureAwait(false) is not { IsRevisioned: true })
            {
                throw;
            }
        }
    }

    public async Task<BrandingReseedPublication> RepublishCurrent(
        string reason,
        string incidentReference,
        string actor,
        CancellationToken ct)
    {
        var current = await LoadRevisioned(ct).ConfigureAwait(false);
        var publication = new BrandingReseedPublication(Guid.CreateVersion7(), current.SourceEpoch, current.SourceRevision);
        var recordedAt = timeProvider.GetUtcNow();
        await Enqueue(current, publication.EventId, recordedAt, ct).ConfigureAwait(false);
        dbContext.Add(BrandingRecoveryAuditRecord.Create(
            "same-epoch-reseed", current.SourceEpoch, current.SourceRevision, publication, reason, incidentReference, actor, recordedAt));
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return publication;
    }

    public async Task<BrandingReseedPublication> RotateEpoch(
        string reason,
        string incidentReference,
        string actor,
        CancellationToken ct)
    {
        var current = await LoadRevisioned(ct).ConfigureAwait(false);
        var (oldEpoch, oldRevision) = (current.SourceEpoch, current.SourceRevision);
        current.RotateEpoch(Guid.CreateVersion7());
        var publication = new BrandingReseedPublication(Guid.CreateVersion7(), current.SourceEpoch, current.SourceRevision);
        var recordedAt = timeProvider.GetUtcNow();
        dbContext.BrandingSettingsRevisions.Add(BrandingSettingsRevisionRecord.Capture(current, recordedAt));
        await Enqueue(current, publication.EventId, recordedAt, ct).ConfigureAwait(false);
        dbContext.Add(BrandingRecoveryAuditRecord.Create(
            "epoch-rotation", oldEpoch, oldRevision, publication, reason, incidentReference, actor, recordedAt));
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return publication;
    }

    private async Task<BrandingSettingsRecord> LoadRevisioned(CancellationToken ct) =>
        await Load(ct).ConfigureAwait(false) is { IsRevisioned: true } current
            ? current
            : throw new InvalidOperationException("Branding revisions have not started; there is no snapshot to recover from.");

    private static BrandingSettingsSnapshot? ToSnapshot(BrandingSettingsRecord? record) =>
        record is { IsRevisioned: true } && record.ToSettings(BrandingFontFamilies.All) is { } settings
            ? new BrandingSettingsSnapshot(settings, record.SourceEpoch, record.SourceRevision)
            : null;

    private Task<BrandingSettingsRecord?> Load(CancellationToken ct) =>
        dbContext.BrandingSettings.SingleOrDefaultAsync(settings => settings.Id == BrandingSettingsRecord.SettingsId, ct);

    private async Task CommitRevision(BrandingSettingsRecord current, CancellationToken ct)
    {
        var committedAt = timeProvider.GetUtcNow();
        dbContext.BrandingSettingsRevisions.Add(BrandingSettingsRevisionRecord.Capture(current, committedAt));
        await Enqueue(current, Guid.CreateVersion7(), committedAt, ct).ConfigureAwait(false);

        try
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (exception is not DbUpdateConcurrencyException
                                                  && exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two writers creating the first row, or the same revision, collide on a key: same outcome as a
            // stale revision. Every other persistence failure propagates unchanged.
            throw new DbUpdateConcurrencyException("Branding settings changed by another writer.", exception);
        }
    }

    private ValueTask Enqueue(BrandingSettingsRecord current, Guid eventId, DateTimeOffset occurredAt, CancellationToken ct) =>
        outbox.Enqueue(
            new BrandingSettingsChangedIntegrationEvent(
                eventId,
                occurredAt,
                current.SourceEpoch,
                current.SourceRevision,
                current.BrandName,
                current.PrimaryColor,
                current.AccentColor,
                current.BackgroundColor,
                current.TextColor,
                current.HeadingFontFamily,
                current.BodyFontFamily,
                current.LogoUri is null ? null : new Uri(current.LogoUri, UriKind.RelativeOrAbsolute)),
            ct);
}
