namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// Represents public media content proxied from the Catalog API.
/// </summary>
/// <param name="Response">The response that owns the media stream.</param>
/// <param name="Content">The media content stream.</param>
/// <param name="ContentType">The media content type.</param>
public sealed record PublicMediaObjectResponse(HttpResponseMessage Response, Stream Content, string ContentType) : IAsyncDisposable
{
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await Content.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            Response.Dispose();
        }
    }
}
