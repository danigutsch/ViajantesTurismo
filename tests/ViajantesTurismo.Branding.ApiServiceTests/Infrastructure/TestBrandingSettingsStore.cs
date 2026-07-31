namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal sealed class TestBrandingSettingsStore : IBrandingSettingsStore
{
    private BrandingSettings? settings;

    public BrandingSettings? Settings => settings;
    public Exception? SaveException { get; set; }

    public Task<BrandingSettings?> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
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
        return Task.CompletedTask;
    }
}
