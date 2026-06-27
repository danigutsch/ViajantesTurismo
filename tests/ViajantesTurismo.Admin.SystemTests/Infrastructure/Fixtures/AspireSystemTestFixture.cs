using Npgsql;
using Projects;
using SharedKernel.Testing;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Fixtures;

public sealed class AspireSystemTestFixture : IAspireSystemTestFixture, IAsyncLifetime, IDisposable
{
    private AspireTestApplication? _app;
    private HttpClient? _apiClient;
    private string? _databaseConnectionString;

    public HttpClient ApiClient => _apiClient ?? throw new InvalidOperationException("Fixture is not initialized.");

    public Uri ApiBaseUri => ApiClient.BaseAddress ?? throw new InvalidOperationException("API client base address is not configured.");

    public Uri WebAppUrl { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _app = await AspireTestApplication.Start<ViajantesTurismo_AppHost>(
            [ResourceNames.Api, ResourceNames.WebApp, ResourceNames.PublicWebApp],
            null,
            TestContext.Current.CancellationToken);

        _apiClient = _app.CreateHttpClient(ResourceNames.Api);
        WebAppUrl = _app.GetEndpoint(ResourceNames.WebApp, "https");
        _databaseConnectionString = await _app.GetConnectionString(ResourceNames.Database, TestContext.Current.CancellationToken);

        using var warmupTimeoutCts = new CancellationTokenSource(AspireTestApplication.DefaultResourceStartupTimeout);
        using var warmupCts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken, warmupTimeoutCts.Token);
        await WarmUpWebApp(warmupCts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _apiClient?.Dispose();
        _apiClient = null;
        _databaseConnectionString = null;

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
    }

    private async Task WarmUpWebApp(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_app);

        using var webClient = _app.CreateHttpClient(ResourceNames.WebApp);
        using var response = await webClient.GetAsync(new Uri("/", UriKind.Relative), ct);
        response.EnsureSuccessStatusCode();
    }
}
