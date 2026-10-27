using PandaBear.Branding;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

/// <summary>
/// Branding settings store that has no saved settings and discards saves.
/// </summary>
internal sealed class EmptyBrandingSettingsStore : IBrandingSettingsStore
{
    /// <summary>
    /// Returns no settings after honoring cancellation.
    /// </summary>
    public Task<BrandingSettings?> GetSettings(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult<BrandingSettings?>(null);
    }

    /// <summary>
    /// Validates the arguments and discards <paramref name="settings"/>.
    /// </summary>
    public Task SaveSettings(BrandingSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ct.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}
