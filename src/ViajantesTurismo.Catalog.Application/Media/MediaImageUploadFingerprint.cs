using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using PandaBear.InputNormalization;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>Derives a private identity from the original bytes and the normalized upload request.</summary>
internal static class MediaImageUploadFingerprint
{
    /// <summary>Hashes a versioned, unambiguously framed set of original content, gallery, and editorial inputs.</summary>
    public static string Create(MediaImageUploadIntakeRequest request, string contentType, ReadOnlySpan<byte> original)
    {
        var framed = new ArrayBufferWriter<byte>();
        Span<byte> originalHash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(original, originalHash);
        var tags = request.Tags ?? [];

        using (var writer = new Utf8JsonWriter(framed))
        {
            writer.WriteStartArray();
            writer.WriteStringValue("catalog-image-upload-v1");
            writer.WriteStringValue(contentType);
            writer.WriteNumberValue(original.Length);
            writer.WriteBase64StringValue(originalHash);
            writer.WriteStringValue(StringSanitizer.Sanitize(request.AltText));
            writer.WriteStringValue(StringSanitizer.Sanitize(request.Caption));
            writer.WriteStringValue(StringSanitizer.Sanitize(request.Attribution));
            writer.WriteStringValue(StringSanitizer.Sanitize(request.Copyright));
            writer.WriteBooleanValue(tags.All(tag => !string.IsNullOrWhiteSpace(StringSanitizer.Sanitize(tag))));
            writer.WriteStartArray();
            foreach (var tag in StringSanitizer.SanitizeCollection(tags))
            {
                writer.WriteStringValue(tag);
            }

            writer.WriteEndArray();
            writer.WriteBooleanValue(request.TourLinks.All(link => link.DisplayOrder >= 0));
            writer.WriteStartArray();
            foreach (var id in request.TourLinks.Select(link => link.CatalogTourId).Order())
            {
                writer.WriteStringValue(id);
            }

            writer.WriteEndArray();
            writer.WriteEndArray();
        }

        return Convert.ToHexString(SHA256.HashData(framed.WrittenSpan));
    }
}
