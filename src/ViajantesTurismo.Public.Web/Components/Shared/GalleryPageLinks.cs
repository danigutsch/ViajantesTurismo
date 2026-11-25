using System.Globalization;
using SharedKernel.Blazor.Site;

namespace ViajantesTurismo.Public.Web.Components.Shared;

/// <summary>Builds localized gallery URLs and labels for the shared components.</summary>
internal sealed class GalleryPageLinks(PublicPageCulture culture)
{
    /// <summary>Gets the gallery index path without a culture prefix.</summary>
    public const string IndexPath = "/gallery";

    /// <summary>Gets the gallery title in the page language.</summary>
    public string Heading => culture.IsPortuguese ? "Galeria" : "Gallery";

    /// <summary>Gets the album index and photo pager label.</summary>
    public string PagerLabel => culture.IsPortuguese ? "Páginas da galeria" : "Gallery pages";

    /// <summary>Gets the previous page label.</summary>
    public string Previous => culture.IsPortuguese ? "Anterior" : "Previous";

    /// <summary>Gets the next page label.</summary>
    public string Next => culture.IsPortuguese ? "Próxima" : "Next";

    /// <summary>Gets the accessible label format for a numbered page link.</summary>
    public string PageLabel => culture.IsPortuguese ? "Página {0}" : "Page {0}";

    /// <summary>Gets the component link for a public gallery path.</summary>
    public LinkTarget Link(string path, int page = 1) => LinkTarget.FromString(culture.Localize(PagePath(path, page)));

    /// <summary>Gets the optional language annotation when a title differs from the page language.</summary>
    public string? TitleLanguage(string language) =>
        string.Equals(language, culture.ContentLanguage.Value, StringComparison.OrdinalIgnoreCase) ? null : language;

    /// <summary>Formats the photo count in the page language.</summary>
    public string PhotoCount(int count)
    {
        var label = culture.IsPortuguese
            ? count == 1 ? "foto" : "fotos"
            : count == 1 ? "photo" : "photos";
        return string.Create(CultureInfo.InvariantCulture, $"{count} {label}");
    }

    /// <summary>Gets the album path without a culture prefix.</summary>
    public static string AlbumPath(string slug) => $"{IndexPath}/{Uri.EscapeDataString(slug)}";

    /// <summary>Appends the page query only after the first page.</summary>
    public static string PagePath(string path, int page) => page == 1
        ? path
        : string.Create(CultureInfo.InvariantCulture, $"{path}?page={page}");
}
