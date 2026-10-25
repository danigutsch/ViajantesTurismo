using Microsoft.Extensions.Options;
using PandaBear.Branding;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.UnitTests.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal static class DocumentBrandingTestData
{
    public static DocumentBrandingSource Http(BrandingSettingsDto settings, BrandingProjectionState? projection = null) =>
        Create(new FakeBrandingApiClient(settings), projection, DocumentBrandingSourceMode.Http);

    public static DocumentBrandingSource Projection(BrandingProjectionState? projection, BrandingProjectionRecovery? recovery = null) =>
        Create(new UnusedBrandingApiClient(), projection, DocumentBrandingSourceMode.Projection, recovery);

    public static BrandingProjectionState ProjectionMatching(BrandingSettingsDto settings, long sourceRevision) => new(
        Guid.CreateVersion7(),
        sourceRevision,
        settings.BrandName,
        settings.PrimaryColor,
        settings.AccentColor,
        settings.BackgroundColor,
        settings.TextColor,
        settings.HeadingFontFamily,
        settings.BodyFontFamily,
        settings.LogoUri is null ? null : new Uri(settings.LogoUri, UriKind.RelativeOrAbsolute));

    private static DocumentBrandingSource Create(
        IBrandingApiClient client,
        BrandingProjectionState? projection,
        DocumentBrandingSourceMode mode,
        BrandingProjectionRecovery? recovery = null) => new(
            client,
            new FakeBrandingProjectionStore { Current = projection, Recovery = recovery },
            Options.Create(new DocumentBrandingOptions { Source = mode }));
}
