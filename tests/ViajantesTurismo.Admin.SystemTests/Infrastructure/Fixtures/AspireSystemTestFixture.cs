using Npgsql;
using Projects;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Fixtures;

public sealed class AspireSystemTestFixture : IAspireSystemTestFixture, IAsyncLifetime, IDisposable
{
    private AspireTestApplication? _app;
    private HttpClient? _apiClient;
    private HttpClient? _catalogApiClient;
    private CatalogToursApiClient? _catalogTours;
    private string? _databaseConnectionString;
    private string? _catalogDatabaseConnectionString;

    public HttpClient ApiClient => _apiClient ?? throw new InvalidOperationException("Fixture is not initialized.");

    public Uri ApiBaseUri => ApiClient.BaseAddress ?? throw new InvalidOperationException("API client base address is not configured.");

    public Uri WebAppUrl { get; private set; } = null!;

    public Uri PublicWebAppUrl { get; private set; } = null!;

    public ICatalogToursApiClient CatalogTours => _catalogTours ?? throw new InvalidOperationException("Fixture is not initialized.");

    public async ValueTask InitializeAsync()
    {
        _app = await AspireTestApplication.Start<ViajantesTurismo_AppHost>(
            [ResourceNames.Api, ResourceNames.WebApp, ResourceNames.PublicWebApp],
            null,
            TestContext.Current.CancellationToken);

        _apiClient = _app.CreateHttpClient(ResourceNames.Api);
        _catalogApiClient = _app.CreateHttpClient(ResourceNames.CatalogApi);
        _catalogTours = new CatalogToursApiClient(_catalogApiClient);
        WebAppUrl = _app.GetEndpoint(ResourceNames.WebApp, "https");
        PublicWebAppUrl = _app.GetEndpoint(ResourceNames.PublicWebApp, "https");
        _databaseConnectionString = await _app.GetConnectionString(ResourceNames.AdminDatabase, TestContext.Current.CancellationToken);
        _catalogDatabaseConnectionString = await _app.GetConnectionString(ResourceNames.CatalogDatabase, TestContext.Current.CancellationToken);

        using var warmupTimeoutCts = new CancellationTokenSource(AspireTestApplication.DefaultResourceStartupTimeout);
        using var warmupCts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, warmupTimeoutCts.Token);
        await WarmUpWebApp(warmupCts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _apiClient?.Dispose();
        _catalogApiClient?.Dispose();
        _apiClient = null;
        _catalogApiClient = null;
        _catalogTours = null;
        _databaseConnectionString = null;
        _catalogDatabaseConnectionString = null;

        if (_app is not null)
        {
            await _app.DisposeAsync();
            _app = null;
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    internal async Task ResetToKnownBaseline(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await PostgreSqlPublicSchemaReset.Reset(connection, ct);

        await using var catalogConnection = new NpgsqlConnection(_catalogDatabaseConnectionString);
        await PostgreSqlPublicSchemaReset.Reset(catalogConnection, ct);
        await PostgreSqlEventSourcingSchemaReset.Reset(catalogConnection, ct);
    }

    private async Task WarmUpWebApp(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_app);

        using var webClient = _app.CreateHttpClient(ResourceNames.WebApp);
        using var response = await webClient.GetAsync(new Uri("/", UriKind.Relative), ct);
        response.EnsureSuccessStatusCode();

        using var publicWebClient = _app.CreateHttpClient(ResourceNames.PublicWebApp);
        using var publicWebResponse = await publicWebClient.GetAsync(new Uri("/", UriKind.Relative), ct);
        publicWebResponse.EnsureSuccessStatusCode();
    }
}
