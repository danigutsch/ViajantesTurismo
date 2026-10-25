using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;
using PandaBear.IntegrationTesting;
using PandaBear.Npgsql.Testing;
using Projects;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>Owns the assembly's six infrastructure containers and isolated application instances.</summary>
[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "DisposeAsync passes the database pool and Aspire host to independent ordered cleanup before aggregating errors.")]
public sealed class HostedTestFixture : IAsyncLifetime
{
    private readonly ConcurrentDictionary<string, Lazy<Task<AspireTestApplication>>> applications = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> conformanceUserIds = new(StringComparer.Ordinal);
    private AspireTestApplication? infrastructure;
    private PostgreSqlTestDatabasePool? databases;
    private string? seaweedAccessKey;
    private string? seaweedSecretKey;

    public AppHostTestConfiguration Configuration { get; } = AppHostTestArguments.CreateConfiguration();
    public string RunId { get; } = Guid.NewGuid().ToString("N");
    public string WorkDirectory { get; } = Path.Combine(Path.GetTempPath(), $"vt-hosted-{Guid.NewGuid():N}");
    public PostgreSqlTestDatabasePool Databases => databases ?? throw new InvalidOperationException("Infrastructure is not initialized.");
    public Uri IdentityProviderEndpoint => Infrastructure.GetEndpoint(ResourceNames.IdentityProvider, "http");
    public Uri ClamAvEndpoint => Infrastructure.GetEndpoint(ResourceNames.ClamAv, ClamAvResource.TcpEndpointName);
    public Uri SeaweedFsEndpoint => Infrastructure.GetEndpoint(ResourceNames.SeaweedFs, SeaweedFsResource.S3EndpointName);
    public string SeaweedAccessKey => seaweedAccessKey ?? throw new InvalidOperationException("Infrastructure is not initialized.");
    public string SeaweedSecretKey => seaweedSecretKey ?? throw new InvalidOperationException("Infrastructure is not initialized.");
    private AspireTestApplication Infrastructure => infrastructure ?? throw new InvalidOperationException("Infrastructure is not initialized.");

    public string Realm(string stack) => stack == "api" ? "viajantes" : $"hosted-{stack}-{RunId}";

    public string ConformanceUserId(string stack) => conformanceUserIds[stack];

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(WorkDirectory);
        var imports = Directory.CreateDirectory(Path.Combine(WorkDirectory, "keycloak"));
        var template = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Keycloak", "viajantes-realm.json"), ct);
        foreach (var stack in new[] { "api", "system", "concurrent-a", "concurrent-b" })
        {
            var realm = JsonNode.Parse(template) ?? throw new InvalidOperationException("The test realm template is empty.");
            realm["realm"] = Realm(stack);
            if (realm["users"] is JsonArray users)
            {
                foreach (var user in users.OfType<JsonObject>())
                {
                    if (stack != "api")
                    {
                        user["id"] = Guid.NewGuid().ToString();
                    }

                    if (user["username"]?.GetValue<string>() == "conformance")
                    {
                        conformanceUserIds[stack] = user["id"]?.GetValue<string>()
                            ?? throw new InvalidOperationException("The conformance user needs an opaque identifier.");
                    }
                }
            }
            await File.WriteAllTextAsync(Path.Combine(imports.FullName, $"{Realm(stack)}-realm.json"), realm.ToJsonString(), ct);
        }

        seaweedAccessKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        seaweedSecretKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var builder = AspireTestApplication.CreateBuilder([.. Configuration.Arguments, $"--DcpPublisher:ResourceNameSuffix=hosted-{RunId}",
            $"--Parameters:seaweedfs-access-key={seaweedAccessKey}", $"--Parameters:seaweedfs-secret-key={seaweedSecretKey}"]);
        var postgres = builder.AddDatabaseServer(includePgWeb: false);
        _ = postgres.AddDatabase("fixture");
        _ = builder.AddCache(includeRedisInsight: false);
        _ = builder.AddClamAv(ResourceNames.ClamAv);
        _ = builder.AddSeaweedFs(ResourceNames.SeaweedFs, "hosted-media");
        var clientSecret = builder.AddParameter(ResourceNames.ManagementWebClientSecret, secret: true);
        var identity = builder.AddRunModeIdentityProvider(clientSecret)
            ?? throw new InvalidOperationException("Test identity provider was not created.");
        foreach (var mount in identity.Resource.Annotations.OfType<ContainerMountAnnotation>().Where(mount => mount.Target == "/opt/keycloak/data/import").ToArray())
        {
            identity.Resource.Annotations.Remove(mount);
        }

        identity.WithBindMount(imports.FullName, "/opt/keycloak/data/import", isReadOnly: true);
        // The shared gateway keeps the same privacy processors as ordinary AppHost execution.
        var observability = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "observability"));
        builder.AddOpenTelemetryCollectorGateway("opentelemetry-collector",
            Path.Combine(observability, "otel-collector", "privacy.yaml"), Path.Combine(observability, "otel-collector", "aspire.yaml"));
        infrastructure = await AspireTestApplication.Start(builder,
            [ResourceNames.DatabaseServer, ResourceNames.Cache, ResourceNames.ClamAv, ResourceNames.SeaweedFs, ResourceNames.IdentityProvider,
                "opentelemetry-collector"], TimeSpan.FromMinutes(3), ct);
        databases = new PostgreSqlTestDatabasePool(await infrastructure.GetConnectionString("fixture", ct));
    }

    public Task<string> GetPostgreSqlConnectionString(CancellationToken ct) => Infrastructure.GetConnectionString("fixture", ct);

    public Task<AspireTestApplication> GetApplication(HostedProfile profile, string stack, CancellationToken ct)
    {
        return applications.GetOrAdd(stack, _ => new Lazy<Task<AspireTestApplication>>(() => StartApplication(profile, stack, ct))).Value;
    }

    private async Task<AspireTestApplication> StartApplication(HostedProfile profile, string stack, CancellationToken ct)
    {
        var admin = await Databases.Create(ct);
        var catalog = await Databases.Create(ct);
        var security = await Databases.Create(ct);
        var values = new Dictionary<string, string?>
        {
            ["HostedTests:SharedInfrastructure"] = "true",
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = admin.ConnectionString,
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = catalog.ConnectionString,
            [$"ConnectionStrings:{ResourceNames.SecurityDatabase}"] = security.ConnectionString,
            [$"ConnectionStrings:{ResourceNames.Cache}"] = await Infrastructure.GetConnectionString(ResourceNames.Cache, ct),
            ["HostedTests:Authority"] = $"{IdentityProviderEndpoint.ToString().TrimEnd('/')}/realms/{Realm(stack)}",
            ["HostedTests:ClamAv:Host"] = ClamAvEndpoint.Host,
            ["HostedTests:ClamAv:Port"] = ClamAvEndpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["HostedTests:SeaweedFs:Endpoint"] = SeaweedFsEndpoint.GetLeftPart(UriPartial.Authority),
            ["HostedTests:SeaweedFs:AccessKey"] = SeaweedAccessKey,
            ["HostedTests:SeaweedFs:SecretKey"] = SeaweedSecretKey,
            ["HostedTests:SeaweedFs:Bucket"] = $"{stack}-{RunId}",
            ["HostedTests:OtlpEndpoint"] = Infrastructure.GetEndpoint("opentelemetry-collector", "grpc").ToString(),
        };
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<ViajantesTurismo_AppHost>(
            [.. Configuration.Arguments, .. profile.ToArguments(), $"--DcpPublisher:ResourceNameSuffix={stack}-{RunId}"],
            (_, settings) =>
            {
                settings.Configuration ??= new ConfigurationManager();
                settings.Configuration.AddInMemoryCollection(values);
            }, ct);
        if (profile is HostedProfile.System)
        {
            var paymentDirectory = Directory.CreateDirectory(Path.Combine(WorkDirectory, "payments"));
            var keys = Directory.CreateDirectory(Path.Combine(paymentDirectory.FullName, "keys"));
            var certificatePath = Path.Combine(paymentDirectory.FullName, "test.pfx");
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=VT isolated system payment test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var now = TimeProvider.System.GetUtcNow();
            using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddDays(1));
            await File.WriteAllBytesAsync(certificatePath, certificate.Export(X509ContentType.Pfx), ct);
            var api = builder.Resources.OfType<ProjectResource>().Single(resource => resource.Name == ResourceNames.Api);
            builder.CreateResourceBuilder(api).WithEnvironment("Payments__Enabled", "true")
                .WithEnvironment("Payments__BusinessTimeZone", "UTC")
                .WithEnvironment("Payments__Protection__KeyDirectory", keys.FullName)
                .WithEnvironment("Payments__Protection__CertificatePath", certificatePath);
        }

        return await AspireTestApplication.Start(builder,
            profile is HostedProfile.System ? [ResourceNames.Api, ResourceNames.WebApp, ResourceNames.PublicWebApp] : [ResourceNames.Api, ResourceNames.CatalogApi],
            TimeSpan.FromMinutes(3), ct);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Assembly cleanup must attempt every application and infrastructure resource and preserve failures.")]
    public async ValueTask DisposeAsync()
    {
        List<Exception> failures = [];
        foreach (var application in applications.Values.Where(application => application.IsValueCreated))
        {
            try
            {
                if (application.Value.IsCompletedSuccessfully)
                {
                    await (await application.Value).DisposeAsync();
                }
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }

        foreach (var resource in new IAsyncDisposable?[] { databases, infrastructure })
        {
            try
            {
                if (resource is not null)
                {
                    await resource.DisposeAsync();
                }
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }

        try
        {
            if (Directory.Exists(WorkDirectory))
            {
                Directory.Delete(WorkDirectory, recursive: true);
            }
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }

        if (failures.Count != 0)
        {
            throw new AggregateException("Hosted test cleanup failed.", failures);
        }
    }
}
