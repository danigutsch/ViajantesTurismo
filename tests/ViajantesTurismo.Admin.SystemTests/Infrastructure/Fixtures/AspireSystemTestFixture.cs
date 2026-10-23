using Aspire.Hosting.Testing;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Npgsql;
using Projects;
using PandaBear.IntegrationTesting;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Fixtures;

public sealed class AspireSystemTestFixture : IAspireSystemTestFixture, IAsyncLifetime
{
    private static readonly TimeSpan SystemResourceStartupTimeout = TimeSpan.FromMinutes(3);

    private AspireTestApplication? _app;
    private readonly string _paymentDirectory = Path.Combine(Path.GetTempPath(), "vt-system-payment-" + Guid.CreateVersion7().ToString("N"));
    private string? _databaseConnectionString;
    private string? _catalogDatabaseConnectionString;

    public Uri WebAppUrl { get; private set; } = null!;

    public Uri PublicWebAppUrl { get; private set; } = null!;

    public string ConformanceUserPassword { get; private set; } = string.Empty;

    internal Uri IdentityProviderEndpoint => (_app ?? throw new InvalidOperationException("Fixture is not initialized."))
        .GetEndpoint(ResourceNames.IdentityProvider, "http");

    /// <inheritdoc />
    public Task<HttpClient> CreateApiClient(CancellationToken ct) =>
        CreateAuthenticatedResourceClient(ResourceNames.Api, ApiAudienceNames.Admin, ct);

    internal Task<HttpClient> CreateCatalogApiClient(CancellationToken ct) =>
        CreateAuthenticatedResourceClient(ResourceNames.CatalogApi, ApiAudienceNames.Catalog, ct);

    internal HttpClient CreateResourceClient(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        return (_app ?? throw new InvalidOperationException("Fixture is not initialized."))
            .CreateHttpClient(resourceName);
    }

    public async ValueTask InitializeAsync()
    {
        var testConfiguration = AppHostTestArguments.CreateConfiguration();
        ConformanceUserPassword = testConfiguration.ConformanceUserPassword;
        string[] appHostArguments =
            [.. testConfiguration.Arguments, .. HostedProfile.System.ToArguments(),
                $"--DcpPublisher:ResourceNameSuffix=system-{Guid.CreateVersion7():N}"];
        Directory.CreateDirectory(_paymentDirectory);
        var keys = Directory.CreateDirectory(Path.Combine(_paymentDirectory, "keys"));
        var certificatePath = Path.Combine(_paymentDirectory, "test.pfx");
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=VT isolated system payment test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var now = TimeProvider.System.GetUtcNow();
        using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddDays(1));
        await File.WriteAllBytesAsync(certificatePath, certificate.Export(X509ContentType.Pfx), TestContext.Current.CancellationToken);
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<ViajantesTurismo_AppHost>(appHostArguments, TestContext.Current.CancellationToken);
        var api = builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == ResourceNames.Api);
        builder.CreateResourceBuilder(api)
            .WithEnvironment("Payments__Enabled", "true")
            .WithEnvironment("Payments__BusinessTimeZone", "UTC")
            .WithEnvironment("Payments__Protection__KeyDirectory", keys.FullName)
            .WithEnvironment("Payments__Protection__CertificatePath", certificatePath);
        _app = await AspireTestApplication.Start(builder,
            [ResourceNames.Api, ResourceNames.WebApp, ResourceNames.PublicWebApp], SystemResourceStartupTimeout,
            TestContext.Current.CancellationToken);

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

        if (_app is not null)
        {
            await _app.DisposeAsync();
            _app = null;
        }

        if (Directory.Exists(_paymentDirectory))
        {
            Directory.Delete(_paymentDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    internal async Task ResetToKnownBaseline(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await PostgreSqlPublicSchemaReset.Reset(connection, ["DocumentAuditRecords"], ct);

        await using var catalogConnection = new NpgsqlConnection(_catalogDatabaseConnectionString);
        await PostgreSqlPublicSchemaReset.Reset(catalogConnection, ct);
        await PostgreSqlEventSourcingSchemaReset.Reset(catalogConnection, ct);
    }

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

    private async Task<HttpClient> CreateAuthenticatedResourceClient(
        string resourceName,
        string audience,
        CancellationToken ct)
    {
        var accessToken = await KeycloakConformanceClient.RequestAccessToken(
            IdentityProviderEndpoint,
            ConformanceUserPassword,
            [audience],
            ct);
        var client = CreateResourceClient(resourceName);
        try
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
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
