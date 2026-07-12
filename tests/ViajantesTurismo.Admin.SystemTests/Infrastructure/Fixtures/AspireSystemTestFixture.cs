using Npgsql;
using Projects;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Contracts.Http;
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

    public string ConformanceUserPassword { get; private set; } = string.Empty;

    public ICatalogToursApiClient CatalogTours => _catalogTours ?? throw new InvalidOperationException("Fixture is not initialized.");

    ICatalogToursApiClient IAspireSystemTestFixture.CatalogTours => CatalogTours;

    public async ValueTask InitializeAsync()
    {
        var testConfiguration = AppHostTestArguments.CreateConfiguration();
        ConformanceUserPassword = testConfiguration.ConformanceUserPassword;
        _app = await AspireTestApplication.Start<ViajantesTurismo_AppHost>(
            [ResourceNames.Api, ResourceNames.WebApp, ResourceNames.PublicWebApp],
            null,
            testConfiguration.Arguments,
            TestContext.Current.CancellationToken);

        _apiClient = _app.CreateHttpClient(ResourceNames.Api);
        _catalogApiClient = _app.CreateHttpClient(ResourceNames.CatalogApi);
        var identityProviderEndpoint = _app.GetEndpoint(ResourceNames.IdentityProvider, "http");
        var accessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            testConfiguration.ConformanceUserPassword,
            [ApiAudienceNames.Admin, ApiAudienceNames.Catalog, ApiAudienceNames.Branding],
            TestContext.Current.CancellationToken);
        _apiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        _catalogApiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
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
        ConformanceUserPassword = string.Empty;
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

    internal async Task RequeueCatalogTransportMessageForAdminTour(Guid adminTourId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        const string sql = """
            UPDATE messaging.transport_messages
            SET "ProcessedAt" = NULL,
                "LastConsumeAttemptAt" = NULL,
                "NextConsumeAttemptAt" = NULL,
                "LastConsumeError" = NULL,
                "ClaimedBy" = NULL,
                "ClaimedUntil" = NULL
            WHERE "ConsumerName" = @consumerName
              AND "EventType" = @eventType
              AND "Payload"::jsonb ->> 'AdminTourId' = @adminTourId;
            """;

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("consumerName", IntegrationEventConsumerNames.Catalog);
        command.Parameters.AddWithValue("eventType", AdminTourCreatedIntegrationEvent.EventType);
        command.Parameters.AddWithValue("adminTourId", adminTourId.ToString("D"));
        var affectedRows = await command.ExecuteNonQueryAsync(ct);

        affectedRows.ShouldBe(1);
    }

    internal async Task WaitForCatalogTransportMessageProcessed(Guid adminTourId, CancellationToken ct)
    {
        await Eventually.Until(
            async probeCt =>
            {
                var status = await ReadCatalogTransportMessageStatus(adminTourId, probeCt);
                if (status.LastConsumeError is not null)
                {
                    throw new InvalidOperationException(status.LastConsumeError);
                }

                return status.ProcessedAt is not null && status.LastConsumeError is null ? "processed" : null;
            },
            TimeSpan.FromSeconds(30),
            ct);
    }

    internal async Task<int> CountCatalogTourEvents(Guid adminTourId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_catalogDatabaseConnectionString);

        const string sql = """
            SELECT COUNT(*)
            FROM event_sourcing.events
            WHERE stream_id = @streamId;
            """;

        await using var connection = new NpgsqlConnection(_catalogDatabaseConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("streamId", $"catalog-tour-{adminTourId:N}");

        return (int)(long)(await command.ExecuteScalarAsync(ct) ?? 0L);
    }

    private async Task<(DateTimeOffset? ProcessedAt, string? LastConsumeError)> ReadCatalogTransportMessageStatus(Guid adminTourId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        const string sql = """
            SELECT "ProcessedAt", "LastConsumeError"
            FROM messaging.transport_messages
            WHERE "ConsumerName" = @consumerName
              AND "EventType" = @eventType
              AND "Payload"::jsonb ->> 'AdminTourId' = @adminTourId;
            """;

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("consumerName", IntegrationEventConsumerNames.Catalog);
        command.Parameters.AddWithValue("eventType", AdminTourCreatedIntegrationEvent.EventType);
        command.Parameters.AddWithValue("adminTourId", adminTourId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return (null, "Catalog transport message was not found.");
        }

        var processedAt = await reader.IsDBNullAsync(0, ct) ? (DateTimeOffset?)null : await reader.GetFieldValueAsync<DateTimeOffset>(0, ct);
        var lastConsumeError = await reader.IsDBNullAsync(1, ct) ? null : await reader.GetFieldValueAsync<string>(1, ct);
        return (processedAt, lastConsumeError);
    }

    private async Task WarmUpWebApp(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_app);

        using var webClient = _app.CreateHttpClient(ResourceNames.WebApp);
        using var response = await webClient.GetAsync(new Uri("/robots.txt", UriKind.Relative), ct);
        response.EnsureSuccessStatusCode();

        using var publicWebClient = _app.CreateHttpClient(ResourceNames.PublicWebApp);
        using var publicWebResponse = await publicWebClient.GetAsync(new Uri("/", UriKind.Relative), ct);
        publicWebResponse.EnsureSuccessStatusCode();
    }
}
