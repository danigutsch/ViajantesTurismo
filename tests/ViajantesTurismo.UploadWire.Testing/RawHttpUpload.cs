namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>
/// Raw HTTP/1.1 client that declares the full body length, sends only a prefix of it, and then aborts the TCP
/// connection, so the server sees a client that vanished mid-upload.
/// </summary>
public sealed class RawHttpUpload : IDisposable
{
    private readonly LoopbackHttpBodyRequest connection = new();

    /// <summary>Connects, sends the headers and the first <paramref name="bytesToSend"/> body bytes.</summary>
    public Task Send(Uri baseUri, string path, string authorization, MultipartPayload payload, int bytesToSend, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Send(baseUri, HttpMethod.Post, path, authorization, payload.ContentType, payload.Body, bytesToSend, ifMatch: null, ct);
    }

    /// <summary>Connects, sends the specified request headers and writes a prefix of the raw body.</summary>
    public async Task Send(
        Uri baseUri,
        HttpMethod method,
        string path,
        string authorization,
        string contentType,
        ReadOnlyMemory<byte> body,
        int bytesToSend,
        string? ifMatch,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentOutOfRangeException.ThrowIfNegative(bytesToSend);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bytesToSend, body.Length);

        using var request = new HttpRequestMessage(method, new Uri(baseUri, path)) { Content = new ByteArrayContent([]) };
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        await connection.Send(request, body[..bytesToSend], body.Length, ct).ConfigureAwait(false);
    }

    /// <summary>Reads the response status line the server sent on this connection.</summary>
    public Task<string> ReadStatusLine(CancellationToken ct) => connection.ReadStatusLine(ct);

    /// <summary>Aborts the connection with a reset instead of a graceful close.</summary>
    public void Abort() => connection.Abort();

    /// <inheritdoc />
    public void Dispose() => connection.Dispose();
}
