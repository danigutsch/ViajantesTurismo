namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Response security policy shared by finalized document artifact download routes.</summary>
public static class DocumentArtifactDelivery
{
    /// <summary>Sandboxes HTML attachments and disallows loading resources.</summary>
    public const string ContentSecurityPolicy = "default-src 'none'; sandbox";
}
