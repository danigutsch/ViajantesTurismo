namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal sealed class TestBrandingSettingsStore : IBrandingSettingsStore
{
    private BrandingSettings? settings;

    public Task<BrandingSettings?> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(settings);
    }

    public Task SaveSettings(BrandingSettings settings, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        this.settings = settings;
        return Task.CompletedTask;
    }
}
