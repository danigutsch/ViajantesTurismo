using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.PublicTheme;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class CatalogApiWebApplicationFactory(
    string? environment,
    TestCatalogTourReadModelStore? tourStore = null,
    TestPublicContentStore? publicContentStore = null,
    TestPublicMediaImageStore? mediaStore = null,
    TestPublicThemeSettingsStore? publicThemeStore = null) : WebApplicationFactory<CatalogApiEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (environment is not null)
        {
            builder.UseEnvironment(environment);
        }

        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IPublicContentStore>(publicContentStore ?? new TestPublicContentStore()));
            services.Replace(ServiceDescriptor.Singleton<IPublicThemeSettingsStore>(publicThemeStore ?? new TestPublicThemeSettingsStore()));
            services.Replace(ServiceDescriptor.Singleton<ICatalogTourReadModelStore>(tourStore ?? new TestCatalogTourReadModelStore()));
            services.Replace(ServiceDescriptor.Singleton<IPublicMediaImageStore>(mediaStore ?? new TestPublicMediaImageStore()));
            services.Replace(ServiceDescriptor.Singleton<IMediaObjectStore>(new TestMediaObjectStore()));
            services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
        });
    }
}

internal sealed class TestMediaObjectStore : IMediaObjectStore
{
    public ValueTask<MediaObjectWriteResult> Put(MediaObjectWriteRequest request, CancellationToken ct) => throw new NotSupportedException();

    public ValueTask<MediaObjectReadResult> OpenRead(string objectKey, CancellationToken ct) => throw new NotSupportedException();

    public ValueTask<bool> Exists(string objectKey, CancellationToken ct) => ValueTask.FromResult(false);

    public ValueTask<IReadOnlyList<string>> ListKeys(string prefix, CancellationToken ct) => ValueTask.FromResult<IReadOnlyList<string>>([]);

    public Uri GetPublicUri(string objectKey) => new($"https://cdn.example/{objectKey}");

    public ValueTask<MediaObjectUploadTicket> CreateUploadUrl(MediaObjectUploadRequest request, CancellationToken ct) => throw new NotSupportedException();

    public ValueTask Delete(string objectKey, CancellationToken ct) => ValueTask.CompletedTask;
}
