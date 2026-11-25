using SharedKernel.AspNetCore;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Applies the Management Web security headers and content security policy.
/// </summary>
internal static class ManagementWebSecurityHeaders
{
    private const string ContentSecurityPolicy = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'nonce-{nonce}'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' ws: wss:";

    /// <summary>
    /// The name of the cascading value that carries the document response's nonce from <c>App</c> through <c>Routes</c> to interactive
    /// components, whose circuit has no response of its own.
    /// </summary>
    internal const string NonceCascadeName = "CspNonce";

    /// <summary>
    /// Adds the security headers middleware with a sandbox policy for document artifacts and the Management Web
    /// content security policy for other requests. Inline scripts and styles, and the
    /// third-party stylesheet, need the response's nonce
    /// (<see cref="ICspNonce" />, registered with <see cref="CspNonceServiceCollectionExtensions.AddCspNonce" />).
    /// </summary>
    public static IApplicationBuilder UseManagementWebSecurityHeaders(this IApplicationBuilder app)
    {
        return app
            .UseWhen(IsDocumentArtifactDownload, branch => branch.UseSecurityHeaders(DocumentArtifactDelivery.ContentSecurityPolicy))
            .UseWhen(context => !IsDocumentArtifactDownload(context), branch => branch.UseSecurityHeaders(ContentSecurityPolicy));
    }

    private static bool IsDocumentArtifactDownload(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == DocumentArtifactProxyEndpoints.DownloadEndpointName;
}
