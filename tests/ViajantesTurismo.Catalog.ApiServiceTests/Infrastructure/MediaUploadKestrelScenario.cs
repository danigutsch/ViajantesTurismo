using Microsoft.AspNetCore.TestHost;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.UploadWire.Testing;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>
/// The Catalog tour image upload endpoint on a real loopback Kestrel server, with a recording scanner, fake stores
/// and a probe that observes the request token and the body reads on the server side.
/// </summary>
internal sealed class MediaUploadKestrelScenario : IAsyncDisposable
{
    /// <summary>A valid 2 by 2 PNG image.</summary>
    public static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAD0lEQVR4nGNgSDsDRXAWAEMEBy263W6BAAAAAElFTkSuQmCC");

    private readonly WebApplicationFactory<CatalogApiHostEntryPoint> root;
    private readonly WebApplicationFactory<CatalogApiHostEntryPoint> factory;

    private MediaUploadKestrelScenario(
        WebApplicationFactory<CatalogApiHostEntryPoint> root,
        WebApplicationFactory<CatalogApiHostEntryPoint> factory,
        Uri baseUri,
        Guid tourId,
        RequestLifetimeProbe probe,
        ScanRecorder scans,
        TestPublicMediaImageStore mediaStore,
        TestMediaObjectStore objectStore)
    {
        this.root = root;
        this.factory = factory;
        BaseUri = baseUri;
        TourId = tourId;
        Probe = probe;
        Scans = scans;
        MediaStore = mediaStore;
        ObjectStore = objectStore;
        Client = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(60) };
        CatalogApiTestHost.ConfigureAuthenticatedClient(Client);
    }

    /// <summary>The loopback address of the server.</summary>
    public Uri BaseUri
    {
        get;
    }

    /// <summary>The seeded tour that images are uploaded to.</summary>
    public Guid TourId
    {
        get;
    }

    /// <summary>The upload route of the seeded tour.</summary>
    public string Route => $"/api/v1/catalog/tours/{TourId}/images";

    /// <summary>An authenticated client for the server.</summary>
    public HttpClient Client
    {
        get;
    }

    /// <summary>Server-side observation of the upload request.</summary>
    public RequestLifetimeProbe Probe
    {
        get;
    }

    /// <summary>The scans the upload scanner received.</summary>
    public ScanRecorder Scans
    {
        get;
    }

    /// <summary>The image metadata store the intake persists into.</summary>
    public TestPublicMediaImageStore MediaStore
    {
        get;
    }

    /// <summary>The object store the intake writes originals into.</summary>
    public TestMediaObjectStore ObjectStore
    {
        get;
    }

    /// <summary>Gets the Authorization header value of the client.</summary>
    public string Authorization => Client.DefaultRequestHeaders.Authorization!.ToString();

    /// <summary>Seeds a tour and starts the server.</summary>
    public static async Task<MediaUploadKestrelScenario> Start(Action<IWebHostBuilder>? configureHost = null)
    {
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(tourId, Guid.CreateVersion7(), "TOUR-UPLOAD", "Upload Tour", "upload-tour", false, 1, TimeProvider.System.GetUtcNow()),
            TestContext.Current.CancellationToken);
        var mediaStore = new TestPublicMediaImageStore();
        var objectStore = new TestMediaObjectStore();
        var probe = new RequestLifetimeProbe($"/tours/{tourId}/images");
        var scans = new ScanRecorder();
        var root = CatalogApiTestHost.Create(tourStore, mediaStore, objectStore);
        var factory = root.WithWebHostBuilder(host =>
        {
            host.ConfigureTestServices(services =>
            {
                RequestProbeStartupFilter.Register(services, probe);
                services.Replace(ServiceDescriptor.Singleton<IMediaUploadScanner>(new RecordingMediaUploadScanner(scans)));
            });
            host.UseRandomLoopbackPort();
            configureHost?.Invoke(host);
        });

        var baseUri = factory.Start();
        return new MediaUploadKestrelScenario(root, factory, baseUri, tourId, probe, scans, mediaStore, objectStore);
    }

    /// <summary>Builds the multipart form for an image file with alt text.</summary>
    public static async Task<MultipartPayload> Form(byte[] image, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        using var file = MultipartPayload.FilePart(image, "image/png");
        form.Add(file, "file", "tour.png");
        using var altText = new StringContent("Sunset over the mountains");
        form.Add(altText, "altText");
        return await MultipartPayload.From(form, ct);
    }

    /// <summary>Returns the number of images persisted for the seeded tour.</summary>
    public async Task<int> PersistedImageCount(CancellationToken ct) => (await MediaStore.ListByTour(TourId, ct)).Count;

    /// <summary>Returns the number of objects written to the object store.</summary>
    public async Task<int> StoredObjectCount(CancellationToken ct) => (await ObjectStore.ListKeys(string.Empty, ct)).Count;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await factory.DisposeAsync();
        await root.DisposeAsync();
    }
}
