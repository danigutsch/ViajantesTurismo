using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Catalog.Domain.PublicContent;

/// <summary>
/// The cultures that public content is written in. Content stores <see cref="ContentCulture"/> language tags; this type
/// names the supported set and resolves requested culture text through <see cref="SupportedCultures"/>.
/// </summary>
public static class PublicContentCultures
{
    private const string UndeterminedLanguageTag = "und";

    /// <summary>Gets United States English, the default culture.</summary>
    public static ContentCulture EnglishUnitedStates
    {
        get;
    } = ContentCulture.Restore("en-US");

    /// <summary>Gets Brazilian Portuguese.</summary>
    public static ContentCulture PortugueseBrazil
    {
        get;
    } = ContentCulture.Restore("pt-BR");

    /// <summary>Gets the public content cultures, with United States English as the default.</summary>
    public static SupportedCultures Supported
    {
        get;
    } = SupportedCultures.Create(EnglishUnitedStates, [EnglishUnitedStates, PortugueseBrazil]).Value;

    /// <summary>Determines whether a culture is exactly one of the supported cultures.</summary>
    /// <param name="culture">The culture, or <see langword="null"/> when none was given.</param>
    /// <returns><see langword="true"/> when the culture is supported.</returns>
    public static bool IsSupported(ContentCulture? culture) => culture is not null && Supported.Contains(culture);

    /// <summary>Reads a language tag, for example from a request body, in canonical casing.</summary>
    /// <param name="tag">The language tag text.</param>
    /// <returns>The culture, or <see langword="null"/> when the text is not a language tag.</returns>
    public static ContentCulture? Parse(string? tag)
    {
        var culture = ContentCulture.Create(tag);
        return culture.IsSuccess ? culture.Value : null;
    }

    /// <summary>Gets the language tag of a supported culture, for example for a telemetry tag with bounded values.</summary>
    /// <param name="culture">The culture.</param>
    /// <returns>The culture name, or <c>und</c> (undetermined) when the culture is missing or not supported.</returns>
    public static string ToLanguageTag(ContentCulture? culture) =>
        culture is not null && Supported.Contains(culture) ? culture.Value : UndeterminedLanguageTag;

    /// <summary>
    /// Resolves requested culture text to a supported culture. Blank text selects the default; otherwise the exact culture, its
    /// neutral parent, or a culture of the same language is chosen, so <c>pt</c> and <c>pt-PT</c> both select Brazilian Portuguese.
    /// </summary>
    /// <param name="requested">The requested culture name.</param>
    /// <returns>The supported culture, or <see langword="null"/> when no supported culture matches.</returns>
    public static ContentCulture? Resolve(string? requested) =>
        string.IsNullOrWhiteSpace(requested) ? Supported.Default : Supported.Resolve(requested);
}
