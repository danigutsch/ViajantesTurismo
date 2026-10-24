using PandaBear.Results;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Defines editable customer-facing Catalog tour presentation values.
/// </summary>
/// <param name="Title">The public tour title.</param>
/// <param name="Slug">The public URL slug.</param>
/// <param name="Summary">The concise customer-facing summary.</param>
/// <param name="Description">The detailed customer-facing description.</param>
/// <param name="Itinerary">The plain-text customer-facing itinerary.</param>
/// <param name="SeoTitle">The optional search-engine title override.</param>
/// <param name="SeoDescription">The optional search-engine description override.</param>
public sealed record CatalogTourPresentationUpdate(
    string Title,
    string Slug,
    string Summary,
    string Description,
    string Itinerary,
    string SeoTitle,
    string SeoDescription)
{
    /// <summary>
    /// Creates a presentation update from values validated by the Catalog domain.
    /// </summary>
    /// <param name="title">The public tour title.</param>
    /// <param name="slug">The public URL slug.</param>
    /// <param name="summary">The concise customer-facing summary.</param>
    /// <param name="description">The detailed customer-facing description.</param>
    /// <param name="itinerary">The plain-text customer-facing itinerary.</param>
    /// <param name="seoTitle">The optional search-engine title override.</param>
    /// <param name="seoDescription">The optional search-engine description override.</param>
    /// <returns>A result containing the sanitized update when valid.</returns>
    public static Result<CatalogTourPresentationUpdate> Create(
        string? title,
        string? slug,
        string? summary,
        string? description,
        string? itinerary,
        string? seoTitle,
        string? seoDescription)
    {
        var presentation = CatalogTourPresentation.Create(
            title,
            slug,
            summary,
            description,
            itinerary,
            seoTitle,
            seoDescription);
        if (presentation.IsFailure)
        {
            return presentation.ConvertError<CatalogTourPresentation, CatalogTourPresentationUpdate>();
        }

        var value = presentation.Value;
        return Result.Ok(new CatalogTourPresentationUpdate(
            value.Title,
            value.Slug,
            value.Summary,
            value.Description,
            value.Itinerary,
            value.SeoTitle,
            value.SeoDescription));
    }
}
