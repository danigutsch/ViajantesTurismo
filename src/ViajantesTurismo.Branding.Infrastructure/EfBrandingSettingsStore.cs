using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PandaBear.Branding;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>EF Core store for Branding settings, revisions, and source recovery.</summary>
/// <param name="dbContext">The Branding database context.</param>
/// <param name="outbox">The outbox that receives Branding integration events.</param>
/// <param name="timeProvider">The clock used for commit and audit timestamps.</param>
internal sealed class EfBrandingSettingsStore(
    BrandingDbContext dbContext,
    [FromKeyedServices(typeof(BrandingDbContext))] IIntegrationEventOutbox outbox,
    TimeProvider timeProvider) : IBrandingSettingsStore, IBrandingSettingsRevisionStore, IBrandingSourceRecovery
{
    /// <inheritdoc />
    public async Task<BrandingSettings?> GetSettings(CancellationToken ct)
    {
        var entity = await Load(ct).ConfigureAwait(false);

        return entity?.ToSettings(BrandingFontFamilies.All);
    }

    /// <inheritdoc />
    public async Task<BrandingSettingsSnapshot?> GetCurrent(CancellationToken ct) =>
        ToSnapshot(await Load(ct).ConfigureAwait(false));

    /// <inheritdoc />
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
            if (settings.LogoUri is not null)
            {
                throw new ArgumentException("Branding settings cannot keep a logo before one is stored.", nameof(settings));
            }

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

    /// <inheritdoc />
    public async Task<BrandingSettingsSnapshot?> ReplaceLogoIfMatch(
        BrandingLogo? logo,
        Func<BrandingSettingsSnapshot?, bool> precondition,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(precondition);

        var existing = await Load(ct).ConfigureAwait(false);
        if (!precondition(ToSnapshot(existing)))
        {
            return null;
        }

        if (existing is not { IsRevisioned: true } || existing.ToSettings(BrandingFontFamilies.All) is not { } current)
        {
            throw new InvalidOperationException("Branding revisions have not started; there are no settings to attach a logo to.");
        }

        if (existing.HasSameLogo(logo?.Sha256))
        {
            return ToSnapshot(existing);
        }

        if (logo is not null && !await dbContext.BrandingLogos.AnyAsync(stored => stored.Sha256 == logo.Sha256, ct).ConfigureAwait(false))
        {
            dbContext.BrandingLogos.Add(BrandingLogoRecord.Create(logo, timeProvider.GetUtcNow()));
        }

        var dto = current.ToDto();
        dto.LogoUri = logo is null ? null : BrandingLogoPath.For(logo.Sha256);
        var updated = BrandingSettings.Create(dto, BrandingFontFamilies.All);
        existing.ApplyLogoRevision(
            updated.TryGetValue(out var settings) ? settings : throw new InvalidOperationException("The generated logo URL is not a valid Branding logo URI."),
            logo?.Sha256);

        await CommitRevision(existing, ct).ConfigureAwait(false);
        return ToSnapshot(await Load(ct).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<BrandingLogo?> GetLogo(string sha256, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sha256);

        var stored = await dbContext.BrandingLogos
            .AsNoTracking()
            .SingleOrDefaultAsync(logo => logo.Sha256 == sha256, ct)
            .ConfigureAwait(false);
        return stored?.ToLogo();
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
            dbContext.ChangeTracker.Clear();
            if (await Load(ct).ConfigureAwait(false) is not { IsRevisioned: true })
            {
                throw;
            }
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <summary>
    /// Two writers creating the first row, or the same revision, collide on a key: same outcome as a stale revision.
    /// Every other persistence failure propagates unchanged.
    /// </summary>
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
            throw new DbUpdateConcurrencyException("Branding settings changed by another writer.", exception);
        }
    }

    private async ValueTask Enqueue(BrandingSettingsRecord current, Guid eventId, DateTimeOffset occurredAt, CancellationToken ct)
    {
        var logo = await LoadLogo(current, ct).ConfigureAwait(false);
        await outbox.Enqueue(
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
                current.LogoUri is null ? null : new Uri(current.LogoUri, UriKind.RelativeOrAbsolute),
                logo?.Sha256,
                logo?.ContentType,
                logo?.ToLogo().Content),
            ct).ConfigureAwait(false);
    }

    private async Task<BrandingLogoRecord?> LoadLogo(BrandingSettingsRecord current, CancellationToken ct)
    {
        if (current.LogoSha256 is null)
        {
            return null;
        }

        return await dbContext.BrandingLogos.FindAsync([current.LogoSha256], ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The stored Branding logo is missing.");
    }
}
