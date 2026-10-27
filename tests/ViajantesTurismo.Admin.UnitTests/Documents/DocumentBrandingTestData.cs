using PandaBear.Branding;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.UnitTests.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Test data for document branding sources backed by a fake branding projection store.</summary>
internal static class DocumentBrandingTestData
{
    /// <summary>Creates a branding source whose projection matches the settings at revision 1.</summary>
    public static DocumentBrandingSource From(BrandingSettingsDto settings) => Projection(ProjectionMatching(settings, 1));

    /// <summary>Creates a branding source whose store holds the given projection and recovery state.</summary>
    public static DocumentBrandingSource Projection(BrandingProjectionState? projection, BrandingProjectionRecovery? recovery = null) =>
        new(new FakeBrandingProjectionStore { Current = projection, Recovery = recovery });

    /// <summary>Builds a branding projection state copying the settings at the given source revision.</summary>
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
