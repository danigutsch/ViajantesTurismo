using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PandaBear.AspNetCore.Authentication.Bff;
using PandaBear.HttpCaching.AspNetCore;
using Polly.Timeout;

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

            HttpCacheHeaders.SetNoStore(httpContext);
            httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return TypedResults.File(artifact.Content.ToArray(), HtmlMediaType, artifact.FileName, enableRangeProcessing: false);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            return TypedResults.Problem("The document artifact is not available for download.", statusCode: StatusCodes.Status409Conflict);
        }
        catch (TimeoutRejectedException)
        {
            return TypedResults.Problem("The document artifact could not be retrieved.", statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return TypedResults.Problem("The document artifact could not be retrieved.", statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (HttpRequestException)
        {
            return TypedResults.Problem("The document artifact could not be retrieved.", statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception exception) when (exception is InvalidOperationException or UserTokenUnavailableException)
        {
            return TypedResults.Problem("The document artifact could not be retrieved.", statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
