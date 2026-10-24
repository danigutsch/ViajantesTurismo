using PandaBear.BuildingBlocks;
using PandaBear.InputNormalization;
using PandaBear.Results;

namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Validated, sanitized customer-facing presentation values for a Catalog tour.
/// </summary>
public sealed class CatalogTourPresentation : ValueObject
{
    private CatalogTourPresentation(
        string title,
        string slug,
        string summary,
        string description,
        string itinerary,
        string seoTitle,
        string seoDescription)
    {
        Title = title;
        Slug = slug;
        Summary = summary;
        Description = description;
        Itinerary = itinerary;
        SeoTitle = seoTitle;
        SeoDescription = seoDescription;
    }

    /// <summary>
    /// Gets the public tour title.
    /// </summary>
    public string Title
    {
        get;
    }

    /// <summary>
    /// Gets the canonical public URL slug.
    /// </summary>
    public string Slug
    {
        get;
    }

    /// <summary>
    /// Gets the concise customer-facing summary.
    /// </summary>
    public string Summary
    {
        get;
    }

    /// <summary>
    /// Gets the detailed customer-facing description.
    /// </summary>
    public string Description
    {
        get;
    }

    /// <summary>
    /// Gets the plain-text customer-facing itinerary.
    /// </summary>
    public string Itinerary
    {
        get;
    }

    /// <summary>
    /// Gets the optional search-engine title override.
    /// </summary>
    public string SeoTitle
    {
        get;
    }

    /// <summary>
    /// Gets the optional search-engine description override.
    /// </summary>
    public string SeoDescription
    {
        get;
    }

    /// <summary>
    /// Sanitizes and validates customer-facing presentation values, normalizing the slug.
    /// </summary>
    /// <param name="title">The public tour title.</param>
    /// <param name="slug">The customer-entered public URL slug.</param>
    /// <param name="summary">The concise customer-facing summary.</param>
    /// <param name="description">The detailed customer-facing description.</param>
    /// <param name="itinerary">The plain-text customer-facing itinerary.</param>
    /// <param name="seoTitle">The optional search-engine title override.</param>
    /// <param name="seoDescription">The optional search-engine description override.</param>
    /// <returns>A result containing the presentation when valid.</returns>
    public static Result<CatalogTourPresentation> Create(
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
            errors[nameof(Title)] = [CatalogTourErrors.TitleRequired()];
        }
        else
        {
            AddLengthError(errors, nameof(Title), sanitizedTitle, CatalogDomainLimits.MaxNameLength);
        }

        if (!CatalogTourSlug.TryNormalize(sanitizedSlug, out var normalizedSlug))
        {
            errors[nameof(Slug)] = [CatalogTourErrors.UnsafeSlug()];
        }

        AddLengthError(errors, nameof(Summary), sanitizedSummary, CatalogDomainLimits.MaxBodyLength);
        AddLengthError(errors, nameof(Description), sanitizedDescription, CatalogDomainLimits.MaxBodyLength);
        AddLengthError(errors, nameof(Itinerary), sanitizedItinerary, CatalogDomainLimits.MaxBodyLength);
        AddLengthError(errors, nameof(SeoTitle), sanitizedSeoTitle, CatalogDomainLimits.MaxNameLength);
        AddLengthError(errors, nameof(SeoDescription), sanitizedSeoDescription, CatalogDomainLimits.MaxBodyLength);

        return errors.Count > 0
            ? CatalogTourErrors.InvalidPresentation(errors)
            : Result.Ok(new CatalogTourPresentation(
                sanitizedTitle,
                normalizedSlug,
                sanitizedSummary,
                sanitizedDescription,
                sanitizedItinerary,
                sanitizedSeoTitle,
                sanitizedSeoDescription));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Title;
        yield return Slug;
        yield return Summary;
        yield return Description;
        yield return Itinerary;
        yield return SeoTitle;
        yield return SeoDescription;
    }

    private static void AddLengthError(
        Dictionary<string, string[]> errors,
        string field,
        string value,
        int maxLength)
    {
        if (value.Length > maxLength)
        {
            errors[field] = [CatalogTourErrors.TextTooLong(field, maxLength)];
        }
    }
}
