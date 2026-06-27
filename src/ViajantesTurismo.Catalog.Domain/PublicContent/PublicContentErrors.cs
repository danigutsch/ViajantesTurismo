using SharedKernel.Results;

namespace ViajantesTurismo.Catalog.Domain.PublicContent;

/// <summary>
/// Provides validation errors for editable public website content.
/// </summary>
public static class PublicContentErrors
{
    /// <summary>
    /// Indicates that a content key is missing.
    /// </summary>
    /// <returns>A validation result.</returns>
    public static Result EmptyKey() => Result.Invalid(
        detail: "Public content key cannot be empty or whitespace.",
        field: "key",
        message: "Key is required.");

    /// <summary>
    /// Indicates that a content key is too long.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="actualLength">The actual length.</param>
    /// <returns>A validation result.</returns>
    public static Result KeyTooLong(int maxLength, int actualLength) => Result.Invalid(
        detail: $"Public content key cannot exceed {maxLength} characters. Received: {actualLength} characters.",
        field: "key",
        message: $"Key cannot exceed {maxLength} characters.");

    /// <summary>
    /// Indicates that a selected language is not supported.
    /// </summary>
    /// <param name="field">The invalid field.</param>
    /// <returns>A validation result.</returns>
    public static Result UnsupportedLanguage(string field) => Result.Invalid(
        detail: $"Public content {field} must be a supported language.",
        field: field,
        message: $"{field} must be a supported language.");

    /// <summary>
    /// Indicates that a language variant is duplicated.
    /// </summary>
    /// <param name="field">The invalid field.</param>
    /// <param name="language">The duplicated language.</param>
    /// <returns>A validation result.</returns>
    public static Result DuplicateVariantLanguage(
        string field,
        PublicContentLanguage language) => Result.Invalid(
            detail: $"Public content {field} contains more than one {language} variant.",
            field: field,
            message: $"{field} contains a duplicate language variant.");

    /// <summary>
    /// Indicates that a supported language variant is missing.
    /// </summary>
    /// <param name="field">The invalid field.</param>
    /// <param name="language">The missing language.</param>
    /// <returns>A validation result.</returns>
    public static Result MissingVariantLanguage(
        string field,
        PublicContentLanguage language) => Result.Invalid(
            detail: $"Public content {field} must include a {language} variant.",
            field: field,
            message: $"{field} is missing a supported language variant.");

    /// <summary>
    /// Indicates that content cannot be published while human review is required.
    /// </summary>
    /// <returns>A validation result.</returns>
    public static Result ReviewRequiredBeforePublishing() => Result.Invalid(
        detail: "Public content cannot be published while any language variant requires human review.",
        field: "publicationState",
        message: "Human review is required before publishing.");

    /// <summary>
    /// Indicates that content text is missing.
    /// </summary>
    /// <param name="field">The invalid field.</param>
    /// <returns>A validation result.</returns>
    public static Result EmptyText(string field) => Result.Invalid(
        detail: $"Public content {field} cannot be empty or whitespace.",
        field: field,
        message: $"{field} is required.");

    /// <summary>
    /// Indicates that content text is too long.
    /// </summary>
    /// <param name="field">The invalid field.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="actualLength">The actual length.</param>
    /// <returns>A validation result.</returns>
    public static Result TextTooLong(string field, int maxLength, int actualLength) => Result.Invalid(
        detail: $"Public content {field} cannot exceed {maxLength} characters. Received: {actualLength} characters.",
        field: field,
        message: $"{field} cannot exceed {maxLength} characters.");
}
