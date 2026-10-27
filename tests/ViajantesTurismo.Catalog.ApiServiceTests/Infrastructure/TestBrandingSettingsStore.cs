using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class TestBrandingSettingsStore : IBrandingSettingsStore, IBrandingSettingsRevisionStore
{
    private BrandingSettings? settings;

    public BrandingSettings? Settings => settings;

    public Guid SourceEpoch { get; } = Guid.CreateVersion7();

    public long SourceRevision
    {
        get; private set;
    }

    public int WriteCount
    {
        get; private set;
    }

    public int ReadCount
    {
        get; private set;
    }

    public Exception? SaveException
    {
        get; set;
    }

    public Task<BrandingSettings?> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ReadCount++;
        return Task.FromResult(settings);
    }

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

    public Task<BrandingSettingsSnapshot?> GetCurrent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Snapshot());
    }

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
