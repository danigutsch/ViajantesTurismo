using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Catalog.Domain.PublicContent;

/// <summary>
/// Maps the stored <see cref="PublicContentLanguage"/> values to and from <see cref="ContentCulture"/> language tags.
/// The enum stays the persisted shape; culture text is resolved through <see cref="SupportedCultures"/>.
/// </summary>
public static class PublicContentCultures
{
    private const string UndeterminedLanguageTag = "und";

    private static readonly ContentCulture EnglishUnitedStates = ContentCulture.Restore("en-US");
    private static readonly ContentCulture PortugueseBrazil = ContentCulture.Restore("pt-BR");

    /// <summary>Gets the public content cultures, with United States English as the default.</summary>
    public static SupportedCultures Supported
    {
        get;
    } = SupportedCultures.Create(EnglishUnitedStates, [EnglishUnitedStates, PortugueseBrazil]).Value;

    /// <summary>Gets the culture of a supported language.</summary>
    /// <param name="language">The public content language.</param>
    /// <returns>The culture, or <see langword="null"/> for <see cref="PublicContentLanguage.None"/> or an undefined value.</returns>
    public static ContentCulture? ToCulture(PublicContentLanguage language) => language switch
    {
        PublicContentLanguage.EnUs => EnglishUnitedStates,
        PublicContentLanguage.PtBr => PortugueseBrazil,
        _ => null
    };

    /// <summary>Gets the language tag of a language, for example for a prompt or a telemetry tag.</summary>
    /// <param name="language">The public content language.</param>
    /// <returns>The culture name, or <c>und</c> (undetermined) when the language has no culture.</returns>
    public static string ToLanguageTag(PublicContentLanguage language) => ToCulture(language)?.Value ?? UndeterminedLanguageTag;

    /// <summary>
    /// Resolves requested culture text to a supported language. Blank text selects the default; otherwise the exact culture, its
    /// neutral parent, or a culture of the same language is chosen, so <c>pt</c> and <c>pt-PT</c> both select Brazilian Portuguese.
    /// </summary>
    /// <param name="requested">The requested culture name.</param>
    /// <returns>The supported language, or <see cref="PublicContentLanguage.None"/> when no supported culture matches.</returns>
    public static PublicContentLanguage Resolve(string? requested)
    {
        var culture = string.IsNullOrWhiteSpace(requested) ? Supported.Default : Supported.Resolve(requested);
        if (culture == EnglishUnitedStates)
        {
            return PublicContentLanguage.EnUs;
        }

        return culture == PortugueseBrazil ? PublicContentLanguage.PtBr : PublicContentLanguage.None;
    }
}
