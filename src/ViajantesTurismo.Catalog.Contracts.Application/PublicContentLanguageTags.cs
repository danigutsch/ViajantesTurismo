namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// The language tags of the cultures that public content is written in. The Catalog API sends and accepts these tags in
/// <c>language</c> and <c>sourceLanguage</c> fields; it accepts any casing and answers in canonical casing.
/// </summary>
public static class PublicContentLanguageTags
{
    /// <summary>United States English, the default culture.</summary>
    public const string EnglishUnitedStates = "en-US";

    /// <summary>Brazilian Portuguese.</summary>
    public const string PortugueseBrazil = "pt-BR";

    /// <summary>Gets the supported tags in display order, the default first.</summary>
    public static IReadOnlyList<string> All
    {
        get;
    } = [EnglishUnitedStates, PortugueseBrazil];
}
