using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using Npgsql;
using PandaBear.IntegrationTesting;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;

public sealed class AspireSystemTestFixture(HostedTestFixture shared) : IAspireSystemTestFixture, IAsyncLifetime
{
    private AspireTestApplication? _app;
    private string? _databaseConnectionString;
    private string? _catalogDatabaseConnectionString;
    private Uri? _identityProviderEndpoint;

    public Uri WebAppUrl { get; private set; } = null!;

    public Uri PublicWebAppUrl { get; private set; } = null!;

    public string ConformanceUserPassword { get; private set; } = string.Empty;

    /// <summary>Gets the identity provider endpoint; throws when the fixture is not initialized.</summary>
    internal Uri IdentityProviderEndpoint => _identityProviderEndpoint ?? throw new InvalidOperationException("Fixture is not initialized.");

    /// <summary>Gets the identity provider realm name used by the system stack.</summary>
    internal string IdentityProviderRealm => shared.Realm("system");

    /// <summary>Gets the identifier of the conformance user in the system stack realm.</summary>
    internal string ConformanceUserId => shared.ConformanceUserId("system");

    /// <inheritdoc />
    public Task<HttpClient> CreateApiClient(CancellationToken ct) =>
        CreateAuthenticatedResourceClient(ResourceNames.Api, ApiAudienceNames.Admin, ct);

    /// <summary>Creates an authenticated HTTP client for the Catalog API resource.</summary>
    internal Task<HttpClient> CreateCatalogApiClient(CancellationToken ct) =>
        CreateAuthenticatedResourceClient(ResourceNames.CatalogApi, ApiAudienceNames.Catalog, ct);

    /// <summary>Creates an HTTP client for the named Aspire resource; throws when the fixture is not initialized.</summary>
    internal HttpClient CreateResourceClient(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        return (_app ?? throw new InvalidOperationException("Fixture is not initialized."))
            .CreateHttpClient(resourceName);
    }

    public async ValueTask InitializeAsync()
    {
        var testConfiguration = shared.Configuration;
        ConformanceUserPassword = testConfiguration.ConformanceUserPassword;
        _app = await shared.GetApplication(HostedProfile.System, "system", TestContext.Current.CancellationToken);
        _identityProviderEndpoint = await shared.GetIdentityProviderEndpoint(TestContext.Current.CancellationToken);

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
        ConformanceUserPassword = string.Empty;
        _databaseConnectionString = null;
        _catalogDatabaseConnectionString = null;

        _app = null;
        await ValueTask.CompletedTask;
        GC.SuppressFinalize(this);
    }

    /// <summary>Resets the Admin and Catalog databases (including event sourcing tables) to an empty baseline, keeping the document audit records.</summary>
    internal async Task ResetToKnownBaseline(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await PostgreSqlPublicSchemaReset.Reset(connection, ["DocumentAuditRecords"], ct);

        await using var catalogConnection = new NpgsqlConnection(_catalogDatabaseConnectionString);
        await PostgreSqlPublicSchemaReset.Reset(catalogConnection, ct);
        await PostgreSqlEventSourcingSchemaReset.Reset(catalogConnection, ct);
    }

    /// <summary>Gets the actor id of the latest rejected not-found document read audit record, or <see langword="null" /> when none exists.</summary>
    internal async Task<string?> GetRejectedDocumentReadAuditActor(Guid documentId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        const string sql = """
            SELECT "ActorId"
            FROM "DocumentAuditRecords"
            WHERE "DocumentId" = @documentId
              AND "Operation" = 'Read'
              AND "Outcome" = 'Rejected'
              AND "ReasonCode" = 'DocumentNotFound'
            ORDER BY "OccurredAtUtc" DESC
            LIMIT 1;
            """;

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("documentId", documentId);
        return await command.ExecuteScalarAsync(ct) as string;
    }

    /// <summary>Clears the processing state of the Catalog transport message for the admin tour so it is consumed again, and asserts exactly one row was reset.</summary>
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

    /// <summary>Waits up to 30 seconds until the Catalog transport message for the admin tour is processed, failing fast on a recorded consume error type.</summary>
    internal async Task WaitForCatalogTransportMessageProcessed(Guid adminTourId, CancellationToken ct)
    {
        await Eventually.Until(
            async ct =>
            {
                var (ProcessedAt, LastConsumeError) = await ReadCatalogTransportMessageStatus(adminTourId, ct);
                if (LastConsumeError is not null)
                {
                    throw new InvalidOperationException($"Catalog transport consume failed with {LastConsumeError}; see the consumer logs.");
                }

                return ProcessedAt is not null && LastConsumeError is null ? "processed" : null;
            },
            TimeSpan.FromSeconds(30),
            ct);
    }

    /// <summary>Counts the events in the Catalog tour event stream of the admin tour.</summary>
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

    private async Task<HttpClient> CreateAuthenticatedResourceClient(
        string resourceName,
        string audience,
        CancellationToken ct)
    {
        var accessToken = await KeycloakConformanceClient.RequestAccessToken(
            IdentityProviderEndpoint,
            ConformanceUserPassword,
            [audience],
            ct, realm: IdentityProviderRealm);
        var client = CreateResourceClient(resourceName);
        try
        {
            client.DefaultRequestHeaders.Authorization =
                new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }
        catch (FormatException)
        {
            client.Dispose();
            throw;
        }
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
