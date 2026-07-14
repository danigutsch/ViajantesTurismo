using Npgsql;
using Projects;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public sealed class ApiFixture : Testing.Integration.IAdminTestHost, IAsyncLifetime
{
    private AspireTestApplication? _app;
    private HttpClient? _client;
    private string? _databaseConnectionString;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture is not initialized.");

    internal HttpClient CreateAnonymousClient()
    {
        ArgumentNullException.ThrowIfNull(_app);

        return _app.CreateHttpClient(ResourceNames.Api);
    }

    public Uri BaseUri => Client.BaseAddress ?? throw new InvalidOperationException("Client base address is not configured.");

    public async ValueTask InitializeAsync()
    {
        var testConfiguration = AppHostTestArguments.CreateConfiguration();
        string[] appHostArguments =
            [.. testConfiguration.Arguments, .. HostedProfile.Admin.ToArguments()];
        _app = await AspireTestApplication.Start<ViajantesTurismo_AppHost>(
            [ResourceNames.Api],
            null,
            appHostArguments,
            TestContext.Current.CancellationToken);
        _client = _app.CreateHttpClient(ResourceNames.Api);
        _databaseConnectionString = await _app.GetConnectionString(ResourceNames.AdminDatabase, TestContext.Current.CancellationToken);
        var identityProviderEndpoint = _app.GetEndpoint(ResourceNames.IdentityProvider, "http");
        var accessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            testConfiguration.ConformanceUserPassword,
            [ApiAudienceNames.Admin],
            TestContext.Current.CancellationToken);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public Task<PostgreSqlTestDatabase> CreateIsolatedPostgreSqlDatabase(CancellationToken ct)
    {
        return PostgreSqlTestDatabase.Create(GetDatabaseConnectionString(), ct);
    }

    public async ValueTask DisposeAsync()
    {
        var client = _client;
        var app = _app;
        _client = null;
        _app = null;
        _databaseConnectionString = null;

        client?.Dispose();

        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async Task ResetToKnownBaseline(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await PostgreSqlPublicSchemaReset.Reset(connection, ct);
    }

    private string GetDatabaseConnectionString()
    {
        return _databaseConnectionString ?? throw new InvalidOperationException("Fixture is not initialized.");
    }
}
