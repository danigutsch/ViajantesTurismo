using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using SharedKernel.IntegrationTesting;
using SharedKernel.Npgsql.Testing;
using SharedKernel.Testing;
using Projects;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>
/// Owns the assembly's six infrastructure containers and isolated application stacks.
/// Nothing starts before a test demands it: the infrastructure Aspire application is composed on the first demand with every
/// container set to explicit start, each container is then started once through <see cref="OnDemandResources{T}"/>, and each
/// application stack is one profile of an <see cref="AspireAppHostFixture{TAppHost}"/> wired to those containers.
/// </summary>
[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "DisposeAsync passes every owned resource to the ordered TestCleanup.")]
public sealed class HostedTestFixture : IAsyncLifetime
{
    /// <summary>Identifies the isolated Admin stack for document endpoint tests.</summary>
    internal const string DocumentApiStack = "document-api";
    /// <summary>Owns the dedicated Catalog inbox inspection database and identity realm.</summary>
    internal const string CatalogInboxOperationsStack = "catalog-inbox-operations";
    /// <summary>Owns the isolated Catalog inbox database and Management browser identity realm.</summary>
    internal const string CatalogInboxBrowserStack = "catalog-inbox-browser";
    /// <summary>Owns the Gallery candidate database and Management browser identity realm.</summary>
    internal const string CatalogGalleryDashboardStack = "catalog-gallery-dashboard";

    private const string CollectorName = "opentelemetry-collector";
    private const string FixtureDatabaseName = "fixture";
    private static readonly TimeSpan StartBudget = TimeSpan.FromMinutes(3);
    private static readonly string[] Stacks =
    [
        "api", DocumentApiStack, CatalogInboxOperationsStack, CatalogInboxBrowserStack,
        CatalogGalleryDashboardStack, "system", "concurrent-a", "concurrent-b"
    ];

    private readonly ConcurrentDictionary<string, string> conformanceUserIds = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, HostedProfile> profiles = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, StackLaunch> preparedLaunches = new(StringComparer.Ordinal);
    private readonly Lazy<TestWorkspace> workspace = new(() => TestWorkspace.Create("vt-hosted"), LazyThreadSafetyMode.ExecutionAndPublication);
    private readonly OnDemandResource<AspireTestApplication> infrastructure;
    private readonly OnDemandResources<StartedContainer> containers;
    private readonly OnDemandResources<StackLaunch> launches;
    private readonly ApplicationStacks stacks;
    private readonly PostgreSqlTestDatabasePool databases;

    public HostedTestFixture()
    {
        infrastructure = new(CreateInfrastructure);
        containers = new(StartContainer);
        launches = new(PrepareLaunch);
        stacks = new ApplicationStacks(this);
        databases = new PostgreSqlTestDatabasePool(GetPostgreSqlConnectionString);
    }

    public AppHostTestConfiguration Configuration { get; } = AppHostTestArguments.CreateConfiguration();
    public string RunId { get; } = Guid.NewGuid().ToString("N");
    public string SeaweedAccessKey { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    public string SeaweedSecretKey { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    /// <summary>Gets the pool of PostgreSQL test databases.</summary>
    internal PostgreSqlTestDatabasePool Databases => databases;
    private string RealmImportDirectory => workspace.Value.Folder("keycloak");

    public string Realm(string stack) => stack == "api" ? "viajantes" : $"hosted-{stack}-{RunId}";

    /// <summary>Gets the opaque identifier of the conformance user; valid once the identity provider has been demanded.</summary>
    public string ConformanceUserId(string stack) => conformanceUserIds.TryGetValue(stack, out var id)
        ? id
        : throw new InvalidOperationException($"The realm for stack '{stack}' is prepared when the identity provider is first demanded.");

    /// <summary>Starts nothing: the infrastructure application, its containers and the workspace are created on demand.</summary>
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async Task<string> GetPostgreSqlConnectionString(CancellationToken ct) =>
        await (await containers.Get(ResourceNames.DatabaseServer, ct)).Application.GetConnectionString(FixtureDatabaseName, ct);

    public async Task<Uri> GetIdentityProviderEndpoint(CancellationToken ct) =>
        (await containers.Get(ResourceNames.IdentityProvider, ct)).Application.GetEndpoint(ResourceNames.IdentityProvider, "http");

    public async Task<Uri> GetClamAvEndpoint(CancellationToken ct) =>
        (await containers.Get(ResourceNames.ClamAv, ct)).Application.GetEndpoint(ResourceNames.ClamAv, ClamAvResource.TcpEndpointName);

    public async Task<Uri> GetSeaweedFsEndpoint(CancellationToken ct) =>
        (await containers.Get(ResourceNames.SeaweedFs, ct)).Application.GetEndpoint(ResourceNames.SeaweedFs, SeaweedFsResource.S3EndpointName);

    /// <summary>Gets the application stack, starting it with the profile on the first demand; every later caller shares it.</summary>
    public async Task<AspireTestApplication> GetApplication(HostedProfile profile, string stack, CancellationToken ct)
    {
        var stackProfile = profiles.GetOrAdd(stack, profile);
        if (stackProfile != profile)
        {
            throw new InvalidOperationException($"The '{stack}' stack runs the {stackProfile} profile, not {profile}.");
        }

        _ = await launches.Get(stack, ct);
        return await stacks.Application(stack, ct);
    }

    /// <summary>Stops the application stacks, drops their databases, stops the containers, then removes the workspace.</summary>
    public async ValueTask DisposeAsync()
    {
        await TestCleanup.DisposeAll(
            null,
            stacks,
            launches,
            databases,
            containers,
            infrastructure,
            workspace.IsValueCreated ? workspace.Value : null);
    }

    private Task<AspireTestApplication> CreateInfrastructure(CancellationToken ct)
    {
        var builder = AspireTestApplication.CreateBuilder(
            new Dictionary<string, string?>
            {
                ["Parameters:seaweedfs-access-key"] = SeaweedAccessKey,
                ["Parameters:seaweedfs-secret-key"] = SeaweedSecretKey,
                [AppHostResourceExtensions.RealmImportDirectoryConfigurationKey] = RealmImportDirectory,
            },
            [.. Configuration.Arguments]);
        var database = builder.AddDatabaseServer(includePgWeb: false).WithExplicitStart();
        _ = database.AddDatabase(FixtureDatabaseName);
        _ = builder.AddCache(includeRedisInsight: false).WithExplicitStart();
        _ = builder.AddClamAv(ResourceNames.ClamAv).WithFreshClam(false).WithExplicitStart();
        _ = builder.AddSeaweedFs(ResourceNames.SeaweedFs, "hosted-media").WithExplicitStart();
        var clientSecret = builder.AddParameter(ResourceNames.ManagementWebClientSecret, secret: true);
        var identity = builder.AddRunModeIdentityProvider(clientSecret)
            ?? throw new InvalidOperationException("Test identity provider was not created.");
        _ = identity.WithExplicitStart();
        // The shared gateway keeps the same privacy processors as ordinary AppHost execution.
        var observability = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "observability"));
        _ = builder.AddOpenTelemetryCollectorGateway(CollectorName,
            Path.Combine(observability, "otel-collector", "privacy.yaml"), Path.Combine(observability, "otel-collector", "aspire.yaml")).WithExplicitStart();
        PodmanContainerBudget.Apply(builder);
        // No resource name is awaited: explicit-start resources stay NotStarted until a test demands them. Keep project resources
        // out of this model: a WaitFor edge to a resource that never starts would block the application start.
        return AspireTestApplication.Start(builder, [], StartBudget, ct);
    }

    private async Task<StartedContainer> StartContainer(string resourceName, CancellationToken ct)
    {
        var application = await infrastructure.Get(ct);
        if (resourceName == ResourceNames.IdentityProvider)
        {
            await PrepareRealmImports(ct);
        }

        await application.StartResource(resourceName, StartBudget, ct);
        if (resourceName == ResourceNames.DatabaseServer)
        {
            await application.WaitForHealthy([FixtureDatabaseName], StartBudget, ct);
        }

        return new StartedContainer(application);
    }

    private async Task PrepareRealmImports(CancellationToken ct)
    {
        var template = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Keycloak", "viajantes-realm.json"), ct);
        foreach (var stack in Stacks)
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

            await File.WriteAllTextAsync(Path.Combine(RealmImportDirectory, $"{Realm(stack)}-realm.json"), realm.ToJsonString(), ct);
        }
    }

    private async Task<StackLaunch> PrepareLaunch(string stack, CancellationToken ct)
    {
        var profile = profiles[stack];
        // Every stack is configured with these services; only the Management web app of the System and Full profiles uses the
        // cache (see SharedProductResourceExtensions in the AppHost), so an Admin stack never starts it.
        var includesCache = profile is not HostedProfile.Admin;
        var server = GetPostgreSqlConnectionString(ct);
        var authority = GetIdentityProviderEndpoint(ct);
        var scanner = GetClamAvEndpoint(ct);
        var storage = GetSeaweedFsEndpoint(ct);
        var telemetry = containers.Get(CollectorName, ct);
        List<Task> starts = [server, authority, scanner, storage, telemetry];
        var redis = includesCache ? containers.Get(ResourceNames.Cache, ct) : null;
        if (redis is not null)
        {
            starts.Add(redis);
        }

        // Concurrent starts take about half the time of sequential ones.
        await Task.WhenAll(starts);
        var identityProviderEndpoint = await authority;
        var clamAvEndpoint = await scanner;
        var seaweedFsEndpoint = await storage;
        var admin = await Databases.Create(ct);
        var catalog = await Databases.Create(ct);
        var security = await Databases.Create(ct);
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["HostedTests:SharedInfrastructure"] = "true",
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = admin.ConnectionString,
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = catalog.ConnectionString,
            [$"ConnectionStrings:{ResourceNames.SecurityDatabase}"] = security.ConnectionString,
            ["AppHost:External:IdentityProvider:Authority"] = $"{identityProviderEndpoint.ToString().TrimEnd('/')}/realms/{Realm(stack)}",
            ["AppHost:External:ClamAv:Host"] = clamAvEndpoint.Host,
            ["AppHost:External:ClamAv:Port"] = clamAvEndpoint.Port.ToString(CultureInfo.InvariantCulture),
            ["AppHost:External:SeaweedFs:Endpoint"] = seaweedFsEndpoint.GetLeftPart(UriPartial.Authority),
            ["AppHost:External:SeaweedFs:AccessKey"] = SeaweedAccessKey,
            ["AppHost:External:SeaweedFs:SecretKey"] = SeaweedSecretKey,
            ["AppHost:External:SeaweedFs:Bucket"] = $"{stack}-{RunId}",
            ["AppHost:External:Otlp:Endpoint"] = (await telemetry).Application.GetEndpoint(CollectorName, "grpc").ToString(),
        };
        if (redis is not null)
        {
            values[$"ConnectionStrings:{ResourceNames.Cache}"] = await (await redis).Application.GetConnectionString(ResourceNames.Cache, ct);
        }

        var launch = new StackLaunch(profile, values);
        preparedLaunches[stack] = launch;
        return launch;
    }

    /// <summary>The infrastructure application once one of its containers is healthy; disposing it is the infrastructure's job.</summary>
    private sealed record StartedContainer(AspireTestApplication Application);

    /// <summary>The profile and AppHost configuration of a stack whose containers and databases are ready.</summary>
    private sealed record StackLaunch(HostedProfile Profile, IReadOnlyDictionary<string, string?> Configuration);

    /// <summary>One AppHost application per stack, wired to the infrastructure through the AppHost's hosted-test configuration keys.</summary>
    private sealed class ApplicationStacks(HostedTestFixture owner) : AspireAppHostFixture<ViajantesTurismo_AppHost>
    {
        protected override string WorkspaceName => "vt-hosted-stacks";

        protected override AppHostLaunch Launch(string profile, string folder)
        {
            var launch = owner.preparedLaunches.TryGetValue(profile, out var prepared)
                ? prepared
                : throw new InvalidOperationException($"The '{profile}' stack starts through HostedTestFixture.GetApplication.");
            var configuration = new Dictionary<string, string?>(launch.Configuration, StringComparer.Ordinal)
            {
                ["Payments:GeneratedDirectory"] = Path.Combine(folder, "payments"),
            };
            string[] healthy = launch.Profile is HostedProfile.System
                ? [ResourceNames.Api, ResourceNames.WebApp, ResourceNames.PublicWebApp]
                : [ResourceNames.Api, ResourceNames.CatalogApi];
            return new AppHostLaunch([.. owner.Configuration.Arguments, .. launch.Profile.ToArguments()], healthy, StartBudget, configuration);
        }
    }
}
