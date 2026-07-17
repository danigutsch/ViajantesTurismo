using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ViajantesTurismo.Management.Web;

/// <summary>Maps Admin-only mediated document artifact delivery endpoints.</summary>
internal static class DocumentArtifactProxyEndpoints
{
    private const string HtmlMediaType = "text/html; charset=utf-8";

    /// <summary>Maps the authenticated document artifact proxy endpoint.</summary>
    public static IEndpointRouteBuilder MapDocumentArtifactProxy(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/documents/{id:guid}/download", Download)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
        return app;
    }

    private static async Task<IResult> Download(
        Guid id,
        [FromServices]
        IDocumentsApiClient documentsApiClient,
        HttpContext httpContext,
        CancellationToken ct)
    {
        try
        {
            var artifact = await documentsApiClient.DownloadFinalizedArtifact(id, ct);
            if (artifact is null)
            {
                return TypedResults.NotFound();
            }

            httpContext.Response.Headers.CacheControl = "no-store";
            httpContext.Response.Headers.Pragma = "no-cache";
            httpContext.Response.Headers.Expires = "0";
            httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return TypedResults.File(artifact.Content.ToArray(), HtmlMediaType, artifact.FileName, enableRangeProcessing: false);
        }
        catch (HttpRequestException)
        {
            return TypedResults.Problem("The document artifact could not be retrieved.", statusCode: StatusCodes.Status502BadGateway);
        }
        catch (InvalidOperationException)
        {
            return TypedResults.Problem("The document artifact could not be retrieved.", statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
