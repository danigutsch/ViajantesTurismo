using PostgreSqlTestDatabase = SharedKernel.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using Npgsql;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Bookings;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Documents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;

public class ApiFixture(HostedTestFixture shared) : ViajantesTurismo.Admin.Testing.Integration.IAdminTestHost, IAsyncLifetime
{
    private static readonly TimeSpan BrandingProjectionTimeout = TimeSpan.FromMinutes(2);

    private AspireTestApplication? _app;
    private HttpClient? _client;
    private string? _databaseConnectionString;
    private string? _operatorConformanceUserPassword;

    /// <summary>Gets the hosted Admin stack used by this fixture.</summary>
    protected virtual string Stack => "api";

    /// <summary>Gets the conformance user's identifier in the fixture's realm.</summary>
    internal string ConformanceUserId => shared.ConformanceUserId(Stack);

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture is not initialized.");

    /// <summary>Creates an HTTP client for the API without the default bearer token.</summary>
    internal HttpClient CreateAnonymousClient()
    {
        ArgumentNullException.ThrowIfNull(_app);

        return _app.CreateHttpClient(ResourceNames.Api);
    }

    public Uri BaseUri => Client.BaseAddress ?? throw new InvalidOperationException("Client base address is not configured.");

    public async ValueTask InitializeAsync()
    {
        var testConfiguration = shared.Configuration;
        _app = await shared.GetApplication(HostedProfile.Admin, Stack, TestContext.Current.CancellationToken);
        _client = _app.CreateHttpClient(ResourceNames.Api);
        _databaseConnectionString = await _app.GetConnectionString(ResourceNames.AdminDatabase, TestContext.Current.CancellationToken);
        await WaitForBrandingProjection(_databaseConnectionString, TestContext.Current.CancellationToken);
        _operatorConformanceUserPassword = testConfiguration.OperatorConformanceUserPassword;
        var identityProviderEndpoint = await shared.GetIdentityProviderEndpoint(TestContext.Current.CancellationToken);
        var accessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            testConfiguration.ConformanceUserPassword,
            [ApiAudienceNames.Admin],
            TestContext.Current.CancellationToken, realm: shared.Realm(Stack));
        _client.DefaultRequestHeaders.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public Task<PostgreSqlTestDatabase> CreateIsolatedPostgreSqlDatabase(CancellationToken ct)
    {
        return shared.Databases.Create(ct);
    }

    /// <summary>Installs the document mutation concurrency scenario for <paramref name="documentId"/>.</summary>
    internal Task<DocumentMutationConcurrencyScenario> CreateDocumentMutationConcurrencyScenario(
        Guid documentId,
        CancellationToken ct) =>
        DocumentMutationConcurrencyScenario.Create(GetDatabaseConnectionString(), documentId, ct);

    /// <summary>Creates the capacity concurrency scenario for two bookings on the same tour.</summary>
    internal Task<BookingCapacityConcurrencyScenario> CreateBookingCapacityConcurrencyScenario(
        Guid firstBookingId,
        Guid secondBookingId,
        CancellationToken ct) =>
        BookingCapacityConcurrencyScenario.Create(
            GetDatabaseConnectionString(),
            firstBookingId,
            secondBookingId,
            ct);

    /// <summary>Installs a database trigger that fails document audit inserts for <paramref name="bookingId"/>.</summary>
    internal Task<DocumentAuditInsertFailureScenario> CreateDocumentAuditInsertFailureScenario(
        Guid bookingId,
        CancellationToken ct) =>
        DocumentAuditInsertFailureScenario.Create(GetDatabaseConnectionString(), bookingId, ct);

    /// <summary>Installs a trigger that fails completion of the idempotency key with a non-retryable error.</summary>
    internal Task<DocumentIdempotencyCompletionFailureScenario> CreateDocumentIdempotencyCompletionFailureScenario(
        string scope,
        Guid idempotencyKey,
        CancellationToken ct) =>
        DocumentIdempotencyCompletionFailureScenario.Create(
            GetDatabaseConnectionString(),
            scope,
            idempotencyKey,
            failOnceWithRetryableError: false,
            ct);

    /// <summary>Installs a trigger that fails completion of the idempotency key once with a retryable error.</summary>
    internal Task<DocumentIdempotencyCompletionFailureScenario> CreateDocumentIdempotencyTransientCompletionFailureScenario(
        string scope,
        Guid idempotencyKey,
        CancellationToken ct) =>
        DocumentIdempotencyCompletionFailureScenario.Create(
            GetDatabaseConnectionString(),
            scope,
            idempotencyKey,
            failOnceWithRetryableError: true,
            ct);

    /// <summary>Creates the scenario that holds a booking cancellation while document persistence waits.</summary>
    internal Task<BookingCancellationAtDocumentPersistenceScenario> CreateBookingCancellationAtDocumentPersistenceScenario(
        Guid bookingId,
        CancellationToken ct) =>
        BookingCancellationAtDocumentPersistenceScenario.Create(GetDatabaseConnectionString(), bookingId, ct);

    /// <summary>Reads the document audit records for <paramref name="documentId"/>.</summary>
    internal async Task<IReadOnlyList<DocumentAuditEntry>> GetDocumentAuditMetadata(Guid documentId, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(GetDatabaseConnectionString());
        return await DocumentAuditMetadataReader.ReadByDocumentId(dataSource, documentId, ct);
    }

    /// <summary>Reads the durable control audit for one fixture-owned scheduler request.</summary>
    internal async Task<IReadOnlyList<(string ActorId, string Action, string Reason)>> GetMaintenanceControlAudit(Guid requestId, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(GetDatabaseConnectionString());
        await using var command = dataSource.CreateCommand(
            """
            SELECT "ActorId", "Action", "Reason"
            FROM document_maintenance.control_audit
            WHERE "RequestId" = @requestId;
            """);
        command.Parameters.AddWithValue("requestId", requestId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? [(reader.GetString(0), reader.GetString(1), reader.GetString(2))]
            : [];
    }

    /// <summary>Reads the not-found rejections of <paramref name="operation"/> recorded since <paramref name="sinceUtc"/>; they name no document.</summary>
    internal async Task<IReadOnlyList<DocumentAuditEntry>> GetUnresolvedDocumentRejections(string operation, DateTime sinceUtc, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(GetDatabaseConnectionString());
        return await DocumentAuditMetadataReader.ReadUnresolvedDocumentRejections(dataSource, operation, sinceUtc, ct);
    }

    /// <summary>Reads the document audit records for <paramref name="bookingId"/>.</summary>
    internal async Task<IReadOnlyList<DocumentAuditEntry>> GetDocumentAuditMetadataForBooking(Guid bookingId, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(GetDatabaseConnectionString());
        return await DocumentAuditMetadataReader.ReadByBookingId(dataSource, bookingId, ct);
    }

    /// <summary>Counts the document drafts stored for <paramref name="bookingId"/>.</summary>
    internal async Task<int> GetDocumentDraftCountForBooking(Guid bookingId, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(GetDatabaseConnectionString());
        await using var command = dataSource.CreateCommand(
            """
            SELECT COUNT(*)
            FROM "DocumentDrafts"
            WHERE "BookingId" = @bookingId;
            """);
        command.Parameters.AddWithValue("bookingId", bookingId);
        var count = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(count, global::System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Creates an HTTP client authenticated as the operator user.</summary>
    internal async Task<HttpClient> CreateOperatorClient(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_app);
        ArgumentException.ThrowIfNullOrWhiteSpace(_operatorConformanceUserPassword);

        var identityProviderEndpoint = await shared.GetIdentityProviderEndpoint(ct);
        var accessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            _operatorConformanceUserPassword,
            [ApiAudienceNames.Admin],
            ct,
            KeycloakConformanceClient.OperatorUsername, shared.Realm(Stack));
        var client = _app.CreateHttpClient(ResourceNames.Api);
        client.DefaultRequestHeaders.Authorization = new global::System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public ValueTask DisposeAsync()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes the fixture's owned client and clears its references to the hosted stack.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
            _client = null;
            _app = null;
            _databaseConnectionString = null;
            _operatorConformanceUserPassword = null;
        }
    }

    // Documents read Branding from the Admin projection, which the Admin worker fills from Catalog-hosted Branding.
    private static async Task WaitForBrandingProjection(string connectionString, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        _ = await Eventually.Until(
            async ct =>
            {
                try
                {
                    await using var command = dataSource.CreateCommand("SELECT EXISTS (SELECT 1 FROM branding_projection.settings)");
                    return await command.ExecuteScalarAsync(ct) is true ? "projected" : null;
                }
                catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UndefinedTable)
                {
                    return null;
                }
            },
            BrandingProjectionTimeout,
            TimeSpan.FromMilliseconds(500),
            ct);
    }

    private string GetDatabaseConnectionString()
    {
        return _databaseConnectionString ?? throw new InvalidOperationException("Fixture is not initialized.");
    }
}
