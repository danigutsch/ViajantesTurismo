using SharedKernel.Results;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Defines editable customer-facing Catalog tour presentation values.
/// </summary>
/// <param name="Title">The public tour title.</param>
/// <param name="Slug">The public URL slug.</param>
/// <param name="IsPublished">Whether the tour is visible on the public website.</param>
public sealed record CatalogTourPresentationUpdate(string Title, string Slug, bool IsPublished)
{
    /// <summary>
    /// Creates a sanitized Catalog tour presentation update.
    /// </summary>
    /// <param name="title">The public tour title.</param>
    /// <param name="slug">The public URL slug.</param>
    /// <param name="isPublished">Whether the tour is visible on the public website.</param>
    /// <returns>A result containing the update when valid.</returns>
    public static Result<CatalogTourPresentationUpdate> Create(string? title, string? slug, bool isPublished)
    {
        var sanitizedTitle = StringSanitizer.Sanitize(title) ?? string.Empty;
        var sanitizedSlug = StringSanitizer.Sanitize(slug) ?? string.Empty;
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(sanitizedTitle))
        {
            errors[nameof(Title)] = ["Title is required."];
        }
        else if (sanitizedTitle.Length > ContractConstants.MaxNameLength)
        {
            errors[nameof(Title)] = [$"Title cannot exceed {ContractConstants.MaxNameLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(sanitizedSlug))
        {
            errors[nameof(Slug)] = ["Slug is required."];
        }
        else if (sanitizedSlug.Length > ContractConstants.MaxSlugLength)
        {
            errors[nameof(Slug)] = [$"Slug cannot exceed {ContractConstants.MaxSlugLength} characters."];
        }

        return errors.Count > 0
            ? Result.Invalid<CatalogTourPresentationUpdate>("Catalog tour presentation is invalid.", errors)
            : Result.Ok(new CatalogTourPresentationUpdate(sanitizedTitle, sanitizedSlug, isPublished));
    }
}
