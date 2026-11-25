using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Defines Catalog-owned normalization and validation for public tour URL slugs.
/// </summary>
public static class CatalogTourSlug
{
    /// <summary>
    /// Creates the deterministic initial slug for a Catalog tour.
    /// </summary>
    /// <param name="identifier">The source Admin tour identifier.</param>
    /// <param name="catalogTourId">The Catalog tour identifier used when the source cannot form a safe path segment.</param>
    /// <returns>A canonical public slug.</returns>
    public static string CreateInitial(string? identifier, Guid catalogTourId)
    {
        return TryNormalize(identifier, out var slug)
            ? slug
            : $"tour-{catalogTourId:N}";
    }

    /// <summary>
    /// Requires a persisted initial slug to be canonical.
    /// </summary>
    /// <param name="initialSlug">The persisted initial slug.</param>
    /// <returns>A canonical initial slug.</returns>
    public static string RequireCanonical(string? initialSlug)
    {
        return IsCanonical(initialSlug)
            ? initialSlug!
            : throw new InvalidOperationException("The persisted Catalog tour slug must be canonical.");
    }

    /// <summary>
    /// Attempts to normalize a user-entered slug to a lowercase ASCII path segment.
    /// </summary>
    /// <param name="value">The user-entered slug.</param>
    /// <param name="slug">The normalized slug when valid.</param>
    /// <returns><see langword="true" /> when the input can be normalized safely; otherwise, <see langword="false" />.</returns>
    public static bool TryNormalize(string? value, out string slug)
    {
        var result = Slug.Create(value, CatalogDomainLimits.MaxSlugLength);
        slug = result.IsSuccess ? result.Value.Value : string.Empty;
        return result.IsSuccess;
    }

    /// <summary>
    /// Gets a value indicating whether a slug is already in its canonical public form.
    /// </summary>
    /// <param name="value">The candidate slug.</param>
    /// <returns><see langword="true" /> when the slug is canonical; otherwise, <see langword="false" />.</returns>
    public static bool IsCanonical(string? value)
    {
        return Slug.IsCanonical(value, CatalogDomainLimits.MaxSlugLength);
    }
}
