using PandaBear.Branding;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.UnitTests.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal static class DocumentBrandingTestData
{
    public static DocumentBrandingSource From(BrandingSettingsDto settings) => Projection(ProjectionMatching(settings, 1));

    public static DocumentBrandingSource Projection(BrandingProjectionState? projection, BrandingProjectionRecovery? recovery = null) =>
        new(new FakeBrandingProjectionStore { Current = projection, Recovery = recovery });

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
        string.IsNullOrEmpty(settings.LogoUri) ? null : new Uri(settings.LogoUri, UriKind.RelativeOrAbsolute));
}
