using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

internal sealed class BrandingProjectionRecord
{
    public static readonly Guid ProjectionId = Guid.Parse("4c5d0b33-6b8a-4c55-9d0e-6c1f4b2a9e10");

    private BrandingProjectionRecord()
    {
        BrandName = string.Empty;
        PrimaryColor = string.Empty;
        AccentColor = string.Empty;
        BackgroundColor = string.Empty;
        TextColor = string.Empty;
        HeadingFontFamily = string.Empty;
        BodyFontFamily = string.Empty;
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid SourceEpoch
    {
        get; private set;
    }

    public long SourceRevision
    {
        get; private set;
    }

    public string BrandName
    {
        get; private set;
    }

    public string PrimaryColor
    {
        get; private set;
    }

    public string AccentColor
    {
        get; private set;
    }

    public string BackgroundColor
    {
        get; private set;
    }

    public string TextColor
    {
        get; private set;
    }

    public string HeadingFontFamily
    {
        get; private set;
    }

    public string BodyFontFamily
    {
        get; private set;
    }

    public string? LogoUri
    {
        get; private set;
    }

    public static BrandingProjectionRecord Create(BrandingProjectionState state)
    {
        var record = new BrandingProjectionRecord { Id = ProjectionId };
        record.ReplaceWith(state);
        return record;
    }

    public void ReplaceWith(BrandingProjectionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        SourceEpoch = state.SourceEpoch;
        SourceRevision = state.SourceRevision;
        BrandName = state.BrandName;
        PrimaryColor = state.PrimaryColor;
        AccentColor = state.AccentColor;
        BackgroundColor = state.BackgroundColor;
        TextColor = state.TextColor;
        HeadingFontFamily = state.HeadingFontFamily;
        BodyFontFamily = state.BodyFontFamily;
        LogoUri = state.LogoUri?.OriginalString;
    }

    public BrandingProjectionState ToState() => new(
        SourceEpoch,
        SourceRevision,
        BrandName,
        PrimaryColor,
        AccentColor,
        BackgroundColor,
        TextColor,
        HeadingFontFamily,
        BodyFontFamily,
        LogoUri is null ? null : new Uri(LogoUri, UriKind.RelativeOrAbsolute));
}
