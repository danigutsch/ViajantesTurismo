using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using Npgsql;
using PandaBear.IntegrationTesting;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Bookings;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public sealed class ApiFixture(HostedTestFixture shared) : Testing.Integration.IAdminTestHost, IAsyncLifetime
{
    private AspireTestApplication? _app;
    private HttpClient? _client;
    private string? _databaseConnectionString;
    private string? _operatorConformanceUserPassword;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture is not initialized.");

    internal HttpClient CreateAnonymousClient()
    {
        ArgumentNullException.ThrowIfNull(_app);

        return _app.CreateHttpClient(ResourceNames.Api);
    }

    public Uri BaseUri => Client.BaseAddress ?? throw new InvalidOperationException("Client base address is not configured.");

    public async ValueTask InitializeAsync()
    {
        var testConfiguration = shared.Configuration;
        _app = await shared.GetApplication(HostedProfile.Admin, "api", TestContext.Current.CancellationToken);
        _client = _app.CreateHttpClient(ResourceNames.Api);
        _databaseConnectionString = await _app.GetConnectionString(ResourceNames.AdminDatabase, TestContext.Current.CancellationToken);
        _operatorConformanceUserPassword = testConfiguration.OperatorConformanceUserPassword;
        var identityProviderEndpoint = shared.IdentityProviderEndpoint;
        var accessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            testConfiguration.ConformanceUserPassword,
            [ApiAudienceNames.Admin],
            TestContext.Current.CancellationToken, realm: shared.Realm("api"));
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public Task<PostgreSqlTestDatabase> CreateIsolatedPostgreSqlDatabase(CancellationToken ct)
    {
        return shared.Databases.Create(ct);
    }

    internal Task<DocumentMutationConcurrencyScenario> CreateDocumentMutationConcurrencyScenario(
        Guid documentId,
        CancellationToken ct) =>
        DocumentMutationConcurrencyScenario.Create(GetDatabaseConnectionString(), documentId, ct);

    internal Task<BookingCapacityConcurrencyScenario> CreateBookingCapacityConcurrencyScenario(
        Guid firstBookingId,
        Guid secondBookingId,
        CancellationToken ct) =>
        BookingCapacityConcurrencyScenario.Create(
            GetDatabaseConnectionString(),
            firstBookingId,
            secondBookingId,
            ct);

    internal Task<DocumentAuditInsertFailureScenario> CreateDocumentAuditInsertFailureScenario(
        Guid bookingId,
        CancellationToken ct) =>
        DocumentAuditInsertFailureScenario.Create(GetDatabaseConnectionString(), bookingId, ct);

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

    internal Task<BookingCancellationAtDocumentPersistenceScenario> CreateBookingCancellationAtDocumentPersistenceScenario(
        Guid bookingId,
        CancellationToken ct) =>
        BookingCancellationAtDocumentPersistenceScenario.Create(GetDatabaseConnectionString(), bookingId, ct);

    internal async Task<IReadOnlyList<DocumentAuditEntry>> GetDocumentAuditMetadata(Guid documentId, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(GetDatabaseConnectionString());
        return await DocumentAuditMetadataReader.ReadByDocumentId(dataSource, documentId, ct);
    }

    internal async Task<IReadOnlyList<DocumentAuditEntry>> GetDocumentAuditMetadataForBooking(Guid bookingId, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(GetDatabaseConnectionString());
        return await DocumentAuditMetadataReader.ReadByBookingId(dataSource, bookingId, ct);
    }

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
        return Convert.ToInt32(count, System.Globalization.CultureInfo.InvariantCulture);
    }

    internal async Task<HttpClient> CreateOperatorClient(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_app);
        ArgumentException.ThrowIfNullOrWhiteSpace(_operatorConformanceUserPassword);

        var identityProviderEndpoint = shared.IdentityProviderEndpoint;
        var accessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            _operatorConformanceUserPassword,
            [ApiAudienceNames.Admin],
            ct,
            KeycloakConformanceClient.OperatorUsername, shared.Realm("api"));
        var client = _app.CreateHttpClient(ResourceNames.Api);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        var client = _client;
        _client = null;
        _app = null;
        _databaseConnectionString = null;
        _operatorConformanceUserPassword = null;

        client?.Dispose();

        await ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private string GetDatabaseConnectionString()
    {
        return _databaseConnectionString ?? throw new InvalidOperationException("Fixture is not initialized.");
    }
}
