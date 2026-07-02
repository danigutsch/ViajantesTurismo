namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Validates media uploads before storage or processing.
/// </summary>
public interface IMediaUploadValidator
{
    /// <summary>
    /// Validates a media upload request.
    /// </summary>
    /// <param name="request">The validation request.</param>
    /// <returns>Validation errors keyed by field name.</returns>
    IReadOnlyDictionary<string, string[]> Validate(MediaUploadValidationRequest request);
}
