using PandaBear.Branding;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class CatalogHostedBrandingStore : IBrandingSettingsStore, IBrandingSettingsRevisionStore
{
    private readonly BrandingSettingsSnapshot snapshot = new(
        BrandingSettings.Create(
            new BrandingSettingsDto
            {
                BrandName = "Viajantes",
                PrimaryColor = "#112233",
                AccentColor = "#445566",
                BackgroundColor = "#FFFFFF",
                TextColor = "#000000",
                HeadingFontFamily = "Inter",
                BodyFontFamily = "Verdana"
            },
            BrandingFontFamilies.All).Value,
        Guid.CreateVersion7(),
        1);

    public Task<BrandingSettings?> GetSettings(CancellationToken ct) => Task.FromResult<BrandingSettings?>(snapshot.Settings);

    public Task SaveSettings(BrandingSettings settings, CancellationToken ct) =>
        throw new InvalidOperationException("Catalog-hosted Branding isolation tests do not write.");

    public Task<BrandingSettingsSnapshot?> GetCurrent(CancellationToken ct) => Task.FromResult<BrandingSettingsSnapshot?>(snapshot);

    public Task<BrandingSettingsSnapshot?> SaveIfMatch(
        BrandingSettings settings,
        Func<BrandingSettingsSnapshot?, bool> precondition,
        CancellationToken ct) =>
        throw new InvalidOperationException("Catalog-hosted Branding isolation tests do not write.");
}
