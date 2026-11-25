namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Contains a mediated finalized document artifact returned by the Admin API.</summary>
public sealed record DocumentArtifactResponse(ReadOnlyMemory<byte> Content, string FileName)
{
    /// <summary>The negotiated media type of the finalized HTML document.</summary>
    public const string MediaType = "text/html";

    /// <summary>The content type of the UTF-8 artifact sent by the download routes.</summary>
    public const string ContentType = MediaType + "; charset=utf-8";
}
