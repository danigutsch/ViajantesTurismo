namespace ViajantesTurismo.Catalog.Domain.Media;

/// <summary>
/// Pixel dimensions for a media image.
/// </summary>
/// <param name="Width">The image width in pixels.</param>
/// <param name="Height">The image height in pixels.</param>
public sealed record MediaImageDimensions(int Width, int Height);
