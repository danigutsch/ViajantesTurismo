namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Validates uploaded media before storage or processing.
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

        if (!HasAllowedSignature(contentType, request.HeaderBytes.Span))
        {
            errors[nameof(request.HeaderBytes)] = ["File signature does not match the allowed content type."];
        }

        return errors;
    }

    private static bool HasAllowedSignature(string contentType, ReadOnlySpan<byte> header)
    {
        return contentType.ToUpperInvariant() switch
        {
            "IMAGE/JPEG" => header is [0xFF, 0xD8, 0xFF, ..],
            "IMAGE/PNG" => header is [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..],
            "IMAGE/WEBP" => header.Length >= 12
                && header[..4].SequenceEqual("RIFF"u8)
                && header[8..12].SequenceEqual("WEBP"u8),
            "IMAGE/AVIF" => header.Length >= 12
                && header[4..8].SequenceEqual("ftyp"u8)
                && (header[8..12].SequenceEqual("avif"u8) || header[8..12].SequenceEqual("avis"u8)),
            _ => false
        };
    }

    private static bool ContainsPathSegments(string fileName)
    {
        return fileName.Contains('/', StringComparison.Ordinal)
            || fileName.Contains('\\', StringComparison.Ordinal)
            || Path.GetFileName(fileName) != fileName;
    }
}
