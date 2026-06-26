using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace ViajantesTurismo.Public.WebTests;

internal sealed class StubHttpClient : HttpClient
{
    private readonly IHost host;

    public StubHttpClient(IHost host) : base(host.GetTestServer().CreateHandler(), disposeHandler: true)
    {
        this.host = host;
        BaseAddress = new Uri("https://catalog.example");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            host.Dispose();
        }

        base.Dispose(disposing);
    }
}
