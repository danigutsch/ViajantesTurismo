using PandaBear.Results;

namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Provides expected business failures for Catalog tour presentation and publication.
/// </summary>
public static class CatalogTourErrors
{
    /// <summary>
    /// Indicates that a published tour must be unpublished before its presentation can change.
    /// </summary>
    /// <returns>A conflict result.</returns>
    public static Result PublishedPresentationChange() => Result.Conflict(
        "Published tours must be unpublished before their presentation can change.");

    /// <summary>
    /// Indicates that a tour lacks the public content required for publication.
    /// </summary>
    /// <returns>A validation result.</returns>
    public static Result PublicationNotReady() => Result.Invalid(
        detail: "Catalog tours require a title, summary, and slug before publication.",
        field: nameof(CatalogTour.Summary),
        message: "Catalog tours require a title, summary, and slug before publication.");

    /// <summary>
    /// Indicates that one or more presentation values are invalid.
    /// </summary>
    /// <param name="errors">The field validation messages.</param>
    /// <returns>A validation result.</returns>
    public static Result<CatalogTourPresentation> InvalidPresentation(IReadOnlyDictionary<string, string[]> errors) =>
        Result.Invalid<CatalogTourPresentation>("Catalog tour presentation is invalid.", errors);

    /// <summary>
    /// Gets the field message for a missing title.
    /// </summary>
    /// <returns>The validation message.</returns>
    public static string TitleRequired() => "Title is required.";

    /// <summary>
    /// Gets the field message for a slug that cannot form a safe public path segment.
    /// </summary>
    /// <returns>The validation message.</returns>
    public static string UnsafeSlug() => "Slug must be a safe URL path segment.";

    /// <summary>
    /// Gets the field message for text that exceeds its maximum length.
    /// </summary>
    /// <param name="field">The invalid field.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <returns>The validation message.</returns>
    public static string TextTooLong(string field, int maxLength) => $"{field} cannot exceed {maxLength} characters.";
}
