using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.IntegrationTesting;
using PandaBear.Npgsql.Testing;
using Projects;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>
/// Owns the assembly's six infrastructure containers and isolated application instances.
/// Nothing starts before a test demands it: the infrastructure Aspire application is composed on the first demand with every
/// container set to explicit start, and each container is then started once, through its own on-demand gate.
/// </summary>
[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "DisposeAsync passes the on-demand resources to independent ordered cleanup before aggregating errors.")]
public sealed class HostedTestFixture : IAsyncLifetime
{
    private const string CollectorName = "opentelemetry-collector";
    private const string FixtureDatabaseName = "fixture";
    private static readonly TimeSpan StartBudget = TimeSpan.FromMinutes(3);

    private readonly ConcurrentDictionary<string, OnDemandResource<AspireTestApplication>> applications = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> conformanceUserIds = new(StringComparer.Ordinal);
    private readonly OnDemandResource<InfrastructureHost> infrastructure;
    private readonly OnDemandResource<string> postgres;
    private readonly OnDemandResource<string> cache;
    private readonly OnDemandResource<Uri> identityProvider;
    private readonly OnDemandResource<Uri> clamAv;
    private readonly OnDemandResource<Uri> seaweedFs;
    private readonly OnDemandResource<Uri> collector;
    private readonly PostgreSqlTestDatabasePool databases;

    public HostedTestFixture()
    {
        // A start may run only through its gate: starting a container twice recreates it and loses its data.
        infrastructure = new(CreateInfrastructure);
        postgres = new(ct => StartResource(ResourceNames.DatabaseServer, application => application.GetConnectionString(FixtureDatabaseName, ct), ct, FixtureDatabaseName));
        cache = new(ct => StartResource(ResourceNames.Cache, application => application.GetConnectionString(ResourceNames.Cache, ct), ct));
        identityProvider = new(async ct =>
        {
            await PrepareRealmImports(ct);
            return await StartResource(ResourceNames.IdentityProvider, application => Task.FromResult(application.GetEndpoint(ResourceNames.IdentityProvider, "http")), ct);
        });
        clamAv = new(ct => StartResource(ResourceNames.ClamAv, application => Task.FromResult(application.GetEndpoint(ResourceNames.ClamAv, ClamAvResource.TcpEndpointName)), ct));
        seaweedFs = new(ct => StartResource(ResourceNames.SeaweedFs, application => Task.FromResult(application.GetEndpoint(ResourceNames.SeaweedFs, SeaweedFsResource.S3EndpointName)), ct));
        collector = new(ct => StartResource(CollectorName, application => Task.FromResult(application.GetEndpoint(CollectorName, "grpc")), ct));
        databases = new PostgreSqlTestDatabasePool(ct => postgres.Get(ct));
    }

    public AppHostTestConfiguration Configuration { get; } = AppHostTestArguments.CreateConfiguration();
    public string RunId { get; } = Guid.NewGuid().ToString("N");
    public string WorkDirectory { get; } = Path.Combine(Path.GetTempPath(), $"vt-hosted-{Guid.NewGuid():N}");
    public string SeaweedAccessKey { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    public string SeaweedSecretKey { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    /// <summary>Gets the pool of PostgreSQL test databases.</summary>
    internal PostgreSqlTestDatabasePool Databases => databases;
    private string RealmImportDirectory => Path.Combine(WorkDirectory, "keycloak");

    public string Realm(string stack) => stack == "api" ? "viajantes" : $"hosted-{stack}-{RunId}";

    /// <summary>Gets the opaque identifier of the conformance user; valid once the identity provider has been demanded.</summary>
    public string ConformanceUserId(string stack) => conformanceUserIds.TryGetValue(stack, out var id)
        ? id
        : throw new InvalidOperationException($"The realm for stack '{stack}' is prepared when the identity provider is first demanded.");

    /// <summary>Starts nothing: the infrastructure application, its containers and the work directory are created on demand.</summary>
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public Task<string> GetPostgreSqlConnectionString(CancellationToken ct) => postgres.Get(ct);

    public Task<Uri> GetIdentityProviderEndpoint(CancellationToken ct) => identityProvider.Get(ct);

    public Task<Uri> GetClamAvEndpoint(CancellationToken ct) => clamAv.Get(ct);

    public Task<Uri> GetSeaweedFsEndpoint(CancellationToken ct) => seaweedFs.Get(ct);

    public Task<AspireTestApplication> GetApplication(HostedProfile profile, string stack, CancellationToken ct)
    {
        return applications.GetOrAdd(stack, _ => new OnDemandResource<AspireTestApplication>(ct => StartApplication(profile, stack, ct))).Get(ct);
    }

    private async Task<InfrastructureHost> CreateInfrastructure(CancellationToken ct)
    {
        var builder = AspireTestApplication.CreateBuilder([.. Configuration.Arguments, $"--DcpPublisher:ResourceNameSuffix=hosted-{RunId}",
            $"--Parameters:seaweedfs-access-key={SeaweedAccessKey}", $"--Parameters:seaweedfs-secret-key={SeaweedSecretKey}"]);
        var database = builder.AddDatabaseServer(includePgWeb: false).WithExplicitStart();
        _ = database.AddDatabase(FixtureDatabaseName);
        _ = builder.AddCache(includeRedisInsight: false).WithExplicitStart();
        _ = builder.AddClamAv(ResourceNames.ClamAv).WithFreshClam(false).WithExplicitStart();
        _ = builder.AddSeaweedFs(ResourceNames.SeaweedFs, "hosted-media").WithExplicitStart();
        var clientSecret = builder.AddParameter(ResourceNames.ManagementWebClientSecret, secret: true);
        var identity = builder.AddRunModeIdentityProvider(clientSecret)
            ?? throw new InvalidOperationException("Test identity provider was not created.");
        foreach (var mount in identity.Resource.Annotations.OfType<ContainerMountAnnotation>().Where(mount => mount.Target == "/opt/keycloak/data/import").ToArray())
        {
            identity.Resource.Annotations.Remove(mount);
        }

        _ = identity.WithBindMount(RealmImportDirectory, "/opt/keycloak/data/import", isReadOnly: true).WithExplicitStart();
        // The shared gateway keeps the same privacy processors as ordinary AppHost execution.
        var observability = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "observability"));
        _ = builder.AddOpenTelemetryCollectorGateway(CollectorName,
            Path.Combine(observability, "otel-collector", "privacy.yaml"), Path.Combine(observability, "otel-collector", "aspire.yaml")).WithExplicitStart();
        // The testing wrapper hides its application, so keep the services Aspire hands out before start for the start commands.
        IServiceProvider? services = null;
        builder.Eventing.Subscribe<BeforeStartEvent>((beforeStart, _) =>
        {
            services = beforeStart.Services;
            return Task.CompletedTask;
        });
        // No resource name is awaited: explicit-start resources stay NotStarted until a test demands them. Keep project resources
        // out of this model: a WaitFor edge to a resource that never starts would block the application start.
        AspireTestApplication application;
        try
        {
            application = await AspireTestApplication.Start(builder, [], StartBudget, ct);
        }
        catch (OperationCanceledException cancellation) when (!ct.IsCancellationRequested)
        {
            // Only disposal cancels ct, so this is the start budget. The gate caches the failure for every borrower, and a bare
            // "A task was canceled." would name neither the application nor the budget.
            throw new TimeoutException($"The infrastructure application did not start within {StartBudget}.", cancellation);
        }

        if (services is null)
        {
            await application.DisposeAsync();
            throw new InvalidOperationException("Aspire did not publish its services before start.");
        }

        return new InfrastructureHost(application, services);
    }

    private async Task<T> StartResource<T>(string resourceName, Func<AspireTestApplication, Task<T>> read, CancellationToken ct, string? databaseName = null)
    {
        var host = await infrastructure.Get(ct);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(StartBudget);
        var awaiting = resourceName;
        try
        {
            var started = await host.Commands.ExecuteCommandAsync(resourceName, KnownResourceCommands.StartCommand, budget.Token);

            // A command canceled by the budget reports that by value. Its success only means that the container was requested.
            budget.Token.ThrowIfCancellationRequested();
            if (!started.Success)
            {
                throw new InvalidOperationException($"Starting '{resourceName}' failed: {started.Message}");
            }

            await host.Notifications.WaitForResourceHealthyAsync(resourceName, budget.Token);
            if (databaseName is not null)
            {
                // Aspire creates the databases of a server once the server is ready, so a healthy server does not yet have its database.
                awaiting = databaseName;
                await host.Notifications.WaitForResourceHealthyAsync(databaseName, budget.Token);
            }
        }
        catch (OperationCanceledException) when (budget.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"'{awaiting}' did not become healthy within {StartBudget}.");
        }

        return await read(host.Application);
    }

    private async Task PrepareRealmImports(CancellationToken ct)
    {
        var imports = Directory.CreateDirectory(RealmImportDirectory);
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
    }

    private async Task<AspireTestApplication> StartApplication(HostedProfile profile, string stack, CancellationToken ct)
    {
        // Every stack is configured with these services; only the Management web app of the System and Full profiles uses the
        // cache (see SharedProductResourceExtensions in the AppHost), so an Admin stack never starts it.
        var includesCache = profile is not HostedProfile.Admin;
        var server = GetPostgreSqlConnectionString(ct);
        var authority = GetIdentityProviderEndpoint(ct);
        var scanner = GetClamAvEndpoint(ct);
        var storage = GetSeaweedFsEndpoint(ct);
        var telemetry = collector.Get(ct);
        var redis = includesCache ? cache.Get(ct) : Task.FromResult(string.Empty);
        // Concurrent starts take about half the time of sequential ones.
        await Task.WhenAll(server, authority, scanner, storage, telemetry, redis);
        var identityProviderEndpoint = await authority;
        var clamAvEndpoint = await scanner;
        var seaweedFsEndpoint = await storage;
        var admin = await Databases.Create(ct);
        var catalog = await Databases.Create(ct);
        var security = await Databases.Create(ct);
        var values = new Dictionary<string, string?>
        {
            ["HostedTests:SharedInfrastructure"] = "true",
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = admin.ConnectionString,
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = catalog.ConnectionString,
            [$"ConnectionStrings:{ResourceNames.SecurityDatabase}"] = security.ConnectionString,
            ["HostedTests:Authority"] = $"{identityProviderEndpoint.ToString().TrimEnd('/')}/realms/{Realm(stack)}",
            ["HostedTests:ClamAv:Host"] = clamAvEndpoint.Host,
            ["HostedTests:ClamAv:Port"] = clamAvEndpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["HostedTests:SeaweedFs:Endpoint"] = seaweedFsEndpoint.GetLeftPart(UriPartial.Authority),
            ["HostedTests:SeaweedFs:AccessKey"] = SeaweedAccessKey,
            ["HostedTests:SeaweedFs:SecretKey"] = SeaweedSecretKey,
            ["HostedTests:SeaweedFs:Bucket"] = $"{stack}-{RunId}",
            ["HostedTests:OtlpEndpoint"] = (await telemetry).ToString(),
        };
        if (includesCache)
        {
            values[$"ConnectionStrings:{ResourceNames.Cache}"] = await redis;
        }

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

        try
        {
            return await AspireTestApplication.Start(builder,
                profile is HostedProfile.System ? [ResourceNames.Api, ResourceNames.WebApp, ResourceNames.PublicWebApp] : [ResourceNames.Api, ResourceNames.CatalogApi],
                StartBudget, ct);
        }
        catch (OperationCanceledException cancellation) when (!ct.IsCancellationRequested)
        {
            // Only disposal cancels ct, so this is the start budget; name the stack instead of caching a bare cancellation.
            throw new TimeoutException($"The '{stack}' application stack did not start within {StartBudget}.", cancellation);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Assembly cleanup must attempt every application and infrastructure resource and preserve failures.")]
    public async ValueTask DisposeAsync()
    {
        List<Exception> failures = [];

        // Applications first, then the pool that drops their databases, then the containers, then the infrastructure host.
        IAsyncDisposable[] ordered = [.. applications.Values, databases, postgres, cache, identityProvider, clamAv, seaweedFs, collector, infrastructure];
        foreach (var resource in ordered)
        {
            try
            {
                await resource.DisposeAsync();
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

    private sealed class InfrastructureHost(AspireTestApplication application, IServiceProvider services) : IAsyncDisposable
    {
        public AspireTestApplication Application { get; } = application;

        public ResourceCommandService Commands { get; } = services.GetRequiredService<ResourceCommandService>();

        public ResourceNotificationService Notifications { get; } = services.GetRequiredService<ResourceNotificationService>();

        public ValueTask DisposeAsync() => Application.DisposeAsync();
    }
}
