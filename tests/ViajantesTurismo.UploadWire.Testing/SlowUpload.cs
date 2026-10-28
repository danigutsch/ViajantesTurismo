namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>A POST whose body is sent in gated chunks; the request is pending until the test releases every chunk.</summary>
public sealed class SlowUpload : IDisposable
{
    /// <summary>Starts the POST; no body byte is sent until a chunk is released.</summary>
    public SlowUpload(HttpClient client, Uri route, MultipartPayload payload, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(payload);

        Content = payload.Gated(chunkSize);
        Response = client.PostAsync(route, Content);
    }

    /// <summary>Gets the gated request content.</summary>
    public GatedChunkContent Content
    {
        get;
    }

    /// <summary>Gets the pending response.</summary>
    public Task<HttpResponseMessage> Response
    {
        get;
    }

    /// <inheritdoc />
    public void Dispose() => Content.Dispose();
}
