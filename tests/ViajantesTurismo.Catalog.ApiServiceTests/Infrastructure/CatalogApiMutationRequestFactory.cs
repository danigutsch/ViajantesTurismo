using System.Text;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Builds mutation requests for catalog endpoints.</summary>
internal static class CatalogApiMutationRequestFactory
{
    /// <summary>Creates a request with a multipart image body for image paths, otherwise an empty JSON body.</summary>
    public static HttpRequestMessage Create(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative));
        if (path.EndsWith("/images", StringComparison.Ordinal))
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]), "file", "tour.png");
            content.Add(new StringContent("Tour image"), "altText");
            request.Content = content;
            return request;
        }

        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        return request;
    }
}
