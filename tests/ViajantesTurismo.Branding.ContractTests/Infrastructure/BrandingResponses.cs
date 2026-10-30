using System.Net;
using System.Net.Http.Headers;
using PandaBear.Testing.Http;

namespace ViajantesTurismo.Branding.ContractTests.Infrastructure;

/// <summary>Builds canned Branding API responses.</summary>
internal static class BrandingResponses
{
    /// <summary>Creates a JSON response with an optional entity tag.</summary>
    public static HttpResponseMessage Json(HttpStatusCode status, string body, string? entityTag = null)
    {
        var response = HttpResponseFactory.Json(body, status);
        if (entityTag is not null)
        {
            response.Headers.ETag = EntityTagHeaderValue.Parse(entityTag);
        }

        return response;
    }
}
