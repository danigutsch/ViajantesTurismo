using System.Net.Http.Headers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using PandaBear.MalwareScanning;
using ViajantesTurismo.Catalog.Api;
using ViajantesTurismo.UploadWire.Testing;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Hosts the Branding logo endpoint on the Catalog application's loopback Kestrel server.</summary>
internal sealed class BrandingLogoKestrelScenario : IAsyncDisposable
{
    private const string PngType = "image/png";
    private const string IfMatch = "*";
    private readonly WebApplicationFactory<CatalogApiHostEntryPoint> root;
    private readonly WebApplicationFactory<CatalogApiHostEntryPoint> factory;
    private readonly Uri baseUri;
    private readonly HttpClient client;

    private BrandingLogoKestrelScenario(
        WebApplicationFactory<CatalogApiHostEntryPoint> root,
        WebApplicationFactory<CatalogApiHostEntryPoint> factory,
        Uri baseUri,
        RequestLifetimeProbe probe,
        TestBrandingSettingsStore store)
    {
        this.root = root;
        this.factory = factory;
        this.baseUri = baseUri;
        Probe = probe;
        Store = store;
        client = new HttpClient { BaseAddress = baseUri, Timeout = WireTimeout.Default };
        BrandingApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
    }

    /// <summary>Gets the server-side request observations.</summary>
    public RequestLifetimeProbe Probe
    {
        get;
    }

    /// <summary>Gets the Branding store used by the endpoint.</summary>
    public TestBrandingSettingsStore Store
    {
        get;
    }

    /// <summary>Starts the Branding endpoint with the supplied scanner and optional body data-rate settings.</summary>
    public static BrandingLogoKestrelScenario Start(IMalwareScanner scanner, MinDataRate? minimumRate = null)
    {
        var store = TestBrandingSettingsStore.Seeded();
        var probe = new RequestLifetimeProbe(BrandingLogoRequests.ManagementLogoPath);
        var root = BrandingApiTestHost.Create(store);
        var factory = root.WithWebHostBuilder(host =>
        {
            host.ConfigureTestServices(services =>
            {
                RequestProbeStartupFilter.Register(services, probe);
                services.Replace(ServiceDescriptor.Singleton(scanner));
            });
            if (minimumRate is not null)
            {
                host.ConfigureServices(services =>
                    services.Configure<KestrelServerOptions>(options => options.Limits.MinRequestBodyDataRate = minimumRate));
            }

            host.UseRandomLoopbackPort();
        });

        return new BrandingLogoKestrelScenario(root, factory, factory.Start(), probe, store);
    }

    /// <summary>Sends the raw logo headers with a declared body length and writes the requested prefix.</summary>
    public Task SendPartialUpload(RawHttpUpload upload, ReadOnlyMemory<byte> body, int bytesToSend, CancellationToken ct) =>
        upload.Send(
            baseUri,
            HttpMethod.Put,
            BrandingLogoRequests.ManagementLogoPath,
            client.DefaultRequestHeaders.Authorization?.ToString()
                ?? throw new InvalidOperationException("The Branding client has no authentication token."),
            PngType,
            body,
            bytesToSend,
            IfMatch,
            ct);

    /// <summary>Starts an authenticated client request and transfers ownership of its content.</summary>
    public PendingBrandingLogoUpload StartUpload(HttpContent content, CancellationToken ct, bool chunked = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, new Uri(BrandingLogoRequests.ManagementLogoPath, UriKind.Relative))
        {
            Content = content,
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(PngType);
        request.Headers.IfMatch.Add(EntityTagHeaderValue.Any);
        request.Headers.TransferEncodingChunked = chunked ? true : null;
        return new PendingBrandingLogoUpload(client, request, ct);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();
        await root.DisposeAsync();
    }
}
