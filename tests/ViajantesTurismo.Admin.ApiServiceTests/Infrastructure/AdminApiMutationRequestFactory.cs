using System.Text;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

internal static class AdminApiMutationRequestFactory
{
    public static HttpRequestMessage Create(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative));
        if (path.StartsWith("/api/v1/customers/import", StringComparison.Ordinal))
        {
            var content = new MultipartFormDataContent();
            var file = new ByteArrayContent([]);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                ContractConstants.CustomerImportTextCsvContentType);
            content.Add(file, "file", "customers.csv");
            request.Content = content;
            return request;
        }

        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        return request;
    }
}
