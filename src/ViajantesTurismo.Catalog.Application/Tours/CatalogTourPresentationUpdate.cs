using SharedKernel.Results;
using ViajantesTurismo.Catalog.Contracts.Application;
using SharedKernel.InputNormalization;
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
    /// Creates a sanitized Catalog tour presentation update.
    /// </summary>
    /// <param name="title">The public tour title.</param>
    /// <param name="slug">The public URL slug.</param>
    /// <param name="summary">The concise customer-facing summary.</param>
    /// <param name="description">The detailed customer-facing description.</param>
    /// <param name="itinerary">The plain-text customer-facing itinerary.</param>
    /// <param name="seoTitle">The optional search-engine title override.</param>
    /// <param name="seoDescription">The optional search-engine description override.</param>
    /// <returns>A result containing the update when valid.</returns>
    public static Result<CatalogTourPresentationUpdate> Create(
        string? title,
        string? slug,
        string? summary,
        string? description,
        string? itinerary,
        string? seoTitle,
        string? seoDescription)
    {
        var sanitizedTitle = StringSanitizer.Sanitize(title) ?? string.Empty;
        var sanitizedSlug = StringSanitizer.Sanitize(slug) ?? string.Empty;
        var sanitizedSummary = StringSanitizer.Sanitize(summary) ?? string.Empty;
        var sanitizedDescription = StringSanitizer.Sanitize(description) ?? string.Empty;
        var sanitizedItinerary = StringSanitizer.Sanitize(itinerary) ?? string.Empty;
        var sanitizedSeoTitle = StringSanitizer.Sanitize(seoTitle) ?? string.Empty;
        var sanitizedSeoDescription = StringSanitizer.Sanitize(seoDescription) ?? string.Empty;
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(sanitizedTitle))
        {
            errors[nameof(Title)] = ["Title is required."];
        }
        else if (sanitizedTitle.Length > ContractConstants.MaxNameLength)
        {
            errors[nameof(Title)] = [$"Title cannot exceed {ContractConstants.MaxNameLength} characters."];
        }

        if (!CatalogTourSlug.TryNormalize(sanitizedSlug, out var normalizedSlug))
        {
            errors[nameof(Slug)] = ["Slug must be a safe URL path segment."];
        }

        AddLengthError(errors, nameof(Summary), sanitizedSummary, ContractConstants.MaxBodyLength);
        AddLengthError(errors, nameof(Description), sanitizedDescription, ContractConstants.MaxBodyLength);
        AddLengthError(errors, nameof(Itinerary), sanitizedItinerary, ContractConstants.MaxBodyLength);
        AddLengthError(errors, nameof(SeoTitle), sanitizedSeoTitle, ContractConstants.MaxNameLength);
        AddLengthError(errors, nameof(SeoDescription), sanitizedSeoDescription, ContractConstants.MaxBodyLength);

        return errors.Count > 0
            ? Result.Invalid<CatalogTourPresentationUpdate>("Catalog tour presentation is invalid.", errors)
            : Result.Ok(new CatalogTourPresentationUpdate(
                sanitizedTitle,
                normalizedSlug,
                sanitizedSummary,
                sanitizedDescription,
                sanitizedItinerary,
                sanitizedSeoTitle,
                sanitizedSeoDescription));
    }

    private static void AddLengthError(
        Dictionary<string, string[]> errors,
        string field,
        string value,
        int maximumLength)
    {
        if (value.Length > maximumLength)
        {
            errors[field] = [$"{field} cannot exceed {maximumLength} characters."];
        }
    }
}
