namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Defines editable customer-facing Catalog tour presentation values.
/// </summary>
/// <param name="Title">The public tour title.</param>
/// <param name="Slug">The public URL slug.</param>
/// <param name="IsPublished">Whether the tour is visible on the public website.</param>
public sealed record CatalogTourPresentationUpdate(string Title, string Slug, bool IsPublished);
