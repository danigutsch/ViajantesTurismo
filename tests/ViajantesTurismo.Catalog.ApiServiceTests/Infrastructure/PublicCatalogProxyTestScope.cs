using PandaBear.ObjectStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Public.Web.Components;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Hosts Catalog and Public.Web frontends in memory to test the public catalog proxy.</summary>
internal sealed class PublicCatalogProxyTestScope : IAsyncDisposable
{
    private readonly TestCatalogTourReadModelStore tours = new();
    private readonly TestPublicMediaImageStore media = new();
    private readonly TestMediaObjectStore objects = new();
    private readonly WebApplicationFactory<CatalogApiHostEntryPoint> catalogRoot;
    private readonly WebApplicationFactory<CatalogApiHostEntryPoint> catalog;
    private readonly List<WebApplicationFactory<App>> frontends = [];
    private readonly bool sensitiveCircuitBreaker;
    private readonly IReadOnlyDictionary<string, string?>? frontendConfiguration;
    private int catalogRequestCount;

    /// <summary>Creates the Catalog host, optionally with a sensitive circuit breaker and extra configuration.</summary>
    public PublicCatalogProxyTestScope(
        bool sensitiveCircuitBreaker = false,
        IReadOnlyDictionary<string, string?>? frontendConfiguration = null,
        IReadOnlyDictionary<string, string?>? catalogConfiguration = null)
    {
        this.sensitiveCircuitBreaker = sensitiveCircuitBreaker;
        this.frontendConfiguration = frontendConfiguration;
        catalogRoot = CatalogApiTestHost.Create(tours, media, objects);
        // Catalog co-hosts the Branding routes Public.Web reads; give them settings without a database.
        var withBranding = BrandingApiTestHost.WithBranding(catalogRoot, TestBrandingSettingsStore.Seeded());
        catalog = catalogConfiguration is null
            ? withBranding
            : withBranding.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
                (_, configuration) => configuration.AddInMemoryCollection(catalogConfiguration)));
    }

    /// <summary>Number of requests that reached the Catalog host.</summary>
    public int CatalogRequestCount => Volatile.Read(ref catalogRequestCount);

    /// <summary>Creates a client for the given frontend replica with the given remote IP address.</summary>
    public HttpClient CreateVisitor(IPAddress address, int frontendReplica = 0)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentOutOfRangeException.ThrowIfNegative(frontendReplica);
        while (frontends.Count <= frontendReplica)
        {
            frontends.Add(CreateFrontend());
        }

        return new HttpClient(frontends[frontendReplica].Server.CreateHandler(
            context => context.Connection.RemoteIpAddress = address))
        {
            BaseAddress = new Uri("https://public.test")
        };
    }

    /// <summary>Creates a client that calls the Catalog host directly from the given IP address.</summary>
    public HttpClient CreateDirectCatalogCaller(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return new HttpClient(CreateCatalogHandler(address))
        {
            BaseAddress = new Uri("http://catalog.test")
        };
    }

    /// <summary>Seeds a published tour with the given number of ready images and returns their ids.</summary>
    public async Task<IReadOnlyList<Guid>> SeedPublishedGallery(int imageCount, CancellationToken ct)
    {
        var tourId = Guid.CreateVersion7();
        await tours.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId, Guid.CreateVersion7(), "TOUR-2026", "A full gallery", "quota-gallery", true, 1,
                TimeProvider.System.GetUtcNow()), ct);
        var imageIds = new List<Guid>();
        for (var imageNumber = 0; imageNumber < imageCount; imageNumber++)
        {
            var image = PublicMediaImageTestFactory.CreateReadyImage(
                tourId, $"gallery/source-{imageNumber}.jpg", $"gallery/image-{imageNumber}-640.jpg", "sha256:gallery",
                $"Gallery image {imageNumber}", imageNumber, imageNumber == 0);
            await media.Upsert(image, ct);
            var payload = "gallery-media"u8.ToArray();
            using var content = new MemoryStream(payload);
            await objects.Put(new ObjectWriteRequest(
                $"gallery/image-{imageNumber}-640.jpg", content, "image/jpeg", payload.Length, "sha256:gallery"), ct);
            imageIds.Add(image.Id);
        }

        return imageIds;
    }

    /// <summary>Disposes the frontends and the Catalog host.</summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var frontend in frontends)
        {
            await frontend.DisposeAsync();
        }

        await catalogRoot.DisposeAsync();
    }

    private WebApplicationFactory<App> CreateFrontend()
    {
        var configuration = frontendConfiguration?.ToDictionary(static pair => pair.Key, static pair => pair.Value) ?? [];
        configuration["PublicWeb:Sitemap:CanonicalOrigin"] = "https://public.test";

        return WebApplicationTestHost.Create<App>(
            "Development",
            services =>
            {
                services.AddHttpClient<IPublicCatalogApiClient, PublicCatalogApiClient>(
                        client => client.BaseAddress = new Uri("http://catalog.test"))
                    .ConfigurePrimaryHttpMessageHandler(() => CreateCatalogHandler(IPAddress.Loopback));
                services.AddHttpClient<IBrandingApiClient, BrandingApiClient>(
                        client => client.BaseAddress = new Uri("http://catalog.test"))
                    .ConfigurePrimaryHttpMessageHandler(() => CreateCatalogHandler(IPAddress.Loopback));
                services.ConfigureAll<HttpStandardResilienceOptions>(options =>
                {
                    // Keep the real retry predicates and circuit breaker without wall-clock retry delays.
                    options.Retry.Delay = TimeSpan.Zero;
                    options.Retry.UseJitter = false;
                    if (sensitiveCircuitBreaker)
                    {
                        options.CircuitBreaker.MinimumThroughput = 2;
                    }
                });
            },
            configuration: configuration);
    }

    private HttpMessageHandler CreateCatalogHandler(IPAddress peerAddress)
    {
        return catalog.Server.CreateHandler(context =>
        {
            Interlocked.Increment(ref catalogRequestCount);
            context.Connection.RemoteIpAddress = peerAddress;
        });
    }
}
