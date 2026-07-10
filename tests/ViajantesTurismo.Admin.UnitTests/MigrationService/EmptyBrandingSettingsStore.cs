using SharedKernel.Branding;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class EmptyBrandingSettingsStore : IBrandingSettingsStore
{
    public Task<BrandingSettings?> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<BrandingSettings?>(null);
    }

    public Task SaveSettings(BrandingSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ct.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}
