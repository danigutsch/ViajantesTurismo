namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Event raised when the editable customer-facing presentation for a Catalog tour changes.
/// </summary>
/// <param name="CatalogTourId">The Catalog tour identifier.</param>
/// <param name="Title">The public tour title.</param>
/// <param name="Slug">The stable public URL slug.</param>
/// <param name="Summary">The concise customer-facing summary.</param>
/// <param name="Description">The detailed customer-facing description.</param>
/// <param name="Itinerary">The plain-text customer-facing itinerary.</param>
/// <param name="SeoTitle">The optional search-engine title override.</param>
/// <param name="SeoDescription">The optional search-engine description override.</param>
public sealed record CatalogTourPresentationChanged(
    Guid CatalogTourId,
    string Title,
    string Slug,
    string Summary,
    string Description,
    string Itinerary,
    string SeoTitle,
    string SeoDescription);
