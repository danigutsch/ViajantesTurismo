using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>Persisted single-row branding projection received from the branding source.</summary>
internal sealed class BrandingProjectionRecord
{
    /// <summary>Fixed key of the single projection row.</summary>
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

    /// <summary>Row key; always <see cref="ProjectionId" />.</summary>
    public Guid Id
    {
        get; private set;
    }

    /// <summary>Epoch of the branding source this state came from.</summary>
    public Guid SourceEpoch
    {
        get; private set;
    }

    /// <summary>Revision within the source epoch; used as the concurrency token.</summary>
    public long SourceRevision
    {
        get; private set;
    }

    /// <summary>Brand display name.</summary>
    public string BrandName
    {
        get; private set;
    }

    /// <summary>Primary color as a hex string.</summary>
    public string PrimaryColor
    {
        get; private set;
    }

    /// <summary>Accent color as a hex string.</summary>
    public string AccentColor
    {
        get; private set;
    }

    /// <summary>Background color as a hex string.</summary>
    public string BackgroundColor
    {
        get; private set;
    }

    /// <summary>Text color as a hex string.</summary>
    public string TextColor
    {
        get; private set;
    }

    /// <summary>Heading font family name.</summary>
    public string HeadingFontFamily
    {
        get; private set;
    }

    /// <summary>Body font family name.</summary>
    public string BodyFontFamily
    {
        get; private set;
    }

    /// <summary>Logo URI as originally written (relative or absolute); null when no logo is set.</summary>
    public string? LogoUri
    {
        get; private set;
    }

    /// <summary>Creates the projection row from a state.</summary>
    public static BrandingProjectionRecord Create(BrandingProjectionState state)
    {
        var record = new BrandingProjectionRecord { Id = ProjectionId };
        record.ReplaceWith(state);
        return record;
    }

    /// <summary>Overwrites all projected values from a state.</summary>
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

    /// <summary>Converts the row back to an application projection state.</summary>
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
