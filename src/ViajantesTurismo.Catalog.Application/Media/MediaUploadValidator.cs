namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Validates the file name, declared content type and length of an upload before its content is read.
/// </summary>
public sealed class MediaUploadValidator(MediaUploadValidationOptions? validationOptions = null) : IMediaUploadValidator
{
    private readonly MediaUploadValidationOptions options = validationOptions ?? new MediaUploadValidationOptions();

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string[]> Validate(MediaUploadValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var contentType = request.ContentType.Trim();
        var extension = Path.GetExtension(request.FileName);

        if (ContainsPathSegments(request.FileName))
        {
            errors[nameof(request.FileName)] = ["File name must not include path segments."];
        }

        if (request.Length <= 0 || request.Length > options.MaxLengthBytes)
        {
            errors[nameof(request.Length)] = [$"Upload length must be between 1 and {options.MaxLengthBytes} bytes."];
        }

        if (!options.AllowedExtensionsByContentType.TryGetValue(contentType, out var allowedExtensions))
        {
            errors[nameof(request.ContentType)] = ["Content type is not allowed."];
        }
        else if (!errors.ContainsKey(nameof(request.FileName)) && !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            errors[nameof(request.FileName)] = ["File extension does not match the allowed content type."];
        }

        return errors;
    }

    private static bool ContainsPathSegments(string fileName)
    {
        return fileName.Contains('/', StringComparison.Ordinal)
            || fileName.Contains('\\', StringComparison.Ordinal)
            || Path.GetFileName(fileName) != fileName;
    }
}
