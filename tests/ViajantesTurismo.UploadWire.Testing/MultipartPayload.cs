using System.Net.Http.Headers;

namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>A serialized multipart/form-data request body with its content type (including the boundary).</summary>
/// <param name="Body">Serialized body bytes.</param>
/// <param name="ContentType">Content-Type header value.</param>
public sealed record MultipartPayload(ReadOnlyMemory<byte> Body, string ContentType)
{
    /// <summary>Serializes the form content to bytes.</summary>
    public static async Task<MultipartPayload> From(MultipartFormDataContent form, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(form);

        ReadOnlyMemory<byte> body = await form.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        return new MultipartPayload(body, form.Headers.ContentType!.ToString());
    }

    /// <summary>Returns content that sends the body in chunks of the given size, each held until released.</summary>
    public GatedChunkContent Gated(int chunkSize) => new(Body, chunkSize, ContentType);

    /// <summary>Returns a part with the file bytes, name and content type.</summary>
    public static ByteArrayContent FilePart(byte[] bytes, string contentType)
    {
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return part;
    }
}
