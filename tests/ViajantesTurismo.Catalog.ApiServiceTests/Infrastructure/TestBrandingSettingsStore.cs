using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>In-memory branding settings store with revision tracking and read/write counters for API tests.</summary>
internal sealed class TestBrandingSettingsStore : IBrandingSettingsStore, IBrandingSettingsRevisionStore
{
    private BrandingSettings? settings;

    /// <summary>Gets the currently stored settings, or null when none were saved or seeded.</summary>
    public BrandingSettings? Settings => settings;

    /// <summary>Gets the epoch identifying this store instance as the source of revisions.</summary>
    public Guid SourceEpoch { get; } = Guid.CreateVersion7();

    /// <summary>Gets the revision, incremented on every successful save.</summary>
    public long SourceRevision
    {
        get; private set;
    }

    /// <summary>Gets the number of successful saves.</summary>
    public int WriteCount
    {
        get; private set;
    }

    /// <summary>Gets the number of settings reads.</summary>
    public int ReadCount
    {
        get; private set;
    }

    /// <summary>Gets or sets an exception thrown by the next saves; null lets saves succeed.</summary>
    public Exception? SaveException
    {
        get; set;
    }

    /// <summary>Returns the stored settings and counts the read.</summary>
    public Task<BrandingSettings?> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ReadCount++;
        return Task.FromResult(settings);
    }

    /// <summary>Stores the settings, bumping revision and write count, unless <see cref="SaveException"/> is set.</summary>
    public Task SaveSettings(BrandingSettings settings, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (SaveException is not null)
        {
            throw SaveException;
        }

        this.settings = settings;
        SourceRevision++;
        WriteCount++;
        return Task.CompletedTask;
    }

    /// <summary>Returns a snapshot of the stored settings, or null when none exist.</summary>
    public Task<BrandingSettingsSnapshot?> GetCurrent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Snapshot());
    }

    /// <summary>Saves only when the precondition accepts the current snapshot; returns the new snapshot, or null when rejected.</summary>
    public async Task<BrandingSettingsSnapshot?> SaveIfMatch(
        BrandingSettings settings,
        Func<BrandingSettingsSnapshot?, bool> precondition,
        CancellationToken ct)
    {
        if (!precondition(Snapshot()))
        {
            return null;
        }

        await SaveSettings(settings, ct);
        return Snapshot();
    }

    /// <summary>Gets the logos stored by content address.</summary>
    public Dictionary<string, BrandingLogo> Logos { get; } = new(StringComparer.Ordinal);

    /// <summary>Stores the logo and points the settings at it, or clears the logo, when the precondition accepts the snapshot.</summary>
    public async Task<BrandingSettingsSnapshot?> ReplaceLogoIfMatch(
        BrandingLogo? logo,
        Func<BrandingSettingsSnapshot?, bool> precondition,
        CancellationToken ct)
    {
        if (!precondition(Snapshot()) || settings is null)
        {
            return null;
        }

        var dto = settings.ToDto();
        dto.LogoUri = logo is null ? null : BrandingLogoPath.For(logo.Sha256);
        if (logo is not null)
        {
            Logos[logo.Sha256] = logo;
        }

        await SaveSettings(BrandingSettings.Create(dto, BrandingDefaults.AllowedFonts).Value, ct);
        return Snapshot();
    }

    /// <summary>Returns the stored logo with the address, or null.</summary>
    public Task<BrandingLogo?> GetLogo(string sha256, CancellationToken ct) =>
        Task.FromResult(Logos.TryGetValue(sha256, out var logo) ? logo : null);

    /// <summary>Creates a store already holding seeded settings at revision 1.</summary>
    public static TestBrandingSettingsStore Seeded()
    {
        var store = new TestBrandingSettingsStore
        {
            settings = BrandingSettingsTestData.CreateSettings("Seeded"),
            SourceRevision = 1
        };
        return store;
    }

    private BrandingSettingsSnapshot? Snapshot() =>
        settings is null ? null : new BrandingSettingsSnapshot(settings, SourceEpoch, SourceRevision);
}
