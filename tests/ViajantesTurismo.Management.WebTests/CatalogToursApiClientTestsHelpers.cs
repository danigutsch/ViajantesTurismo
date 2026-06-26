using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace ViajantesTurismo.Management.WebTests;

internal static class CatalogToursApiClientTestsHelpers
{
    public static StubHttpClient CreateClient(Func<HttpRequest, HttpResponseMessage> handler)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder => builder
                .UseTestServer()
                .Configure(app => app.Run(async context =>
                {
                    using var response = handler(context.Request);

                    context.Response.StatusCode = (int)response.StatusCode;

                    if (response.Content is not null)
                    {
                        context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
                        var content = await response.Content.ReadAsStringAsync(context.RequestAborted);
                        await context.Response.WriteAsync(content, context.RequestAborted);
                    }
                })))
            .Start();

        return new StubHttpClient(host);
    }

    public static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
