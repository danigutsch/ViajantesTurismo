namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Represents an image in a public Catalog tour gallery.
/// </summary>
/// <param name="Uri">The public image URI.</param>
/// <param name="AltText">The accessible image description.</param>
/// <param name="Caption">An optional display caption.</param>
public sealed record CatalogTourImageDto(
    Uri Uri,
    string AltText,
    string? Caption);
