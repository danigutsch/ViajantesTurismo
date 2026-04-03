using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;

public sealed class E2EFixture : IAsyncLifetime
{
    private const string LoopbackAddress = "http://127.0.0.1:0";
    private static readonly TimeSpan ResourceStartupTimeout = TimeSpan.FromSeconds(60);

    private readonly DistributedApplication _app;
    private ApiFactory? _apiFactory;
    private string? _databaseConnectionString;
    private WebFactory? _webFactory;

    public E2EFixture()
    {
        var options = new DistributedApplicationOptions
        {
            AssemblyName = typeof(E2EFixture).Assembly.FullName,
            DisableDashboard = true
        };
        var appBuilder = new DistributedApplicationBuilder(options);

        DatabaseServer = appBuilder.AddPostgres(ResourceNames.DatabaseServer);
        Database = DatabaseServer.AddDatabase(ResourceNames.Database);
        Cache = appBuilder.AddRedis(ResourceNames.Cache);

        _app = appBuilder.Build();
    }

    public IResourceBuilder<PostgresServerResource> DatabaseServer { get; }
    public IResourceBuilder<PostgresDatabaseResource> Database { get; }
    public IResourceBuilder<RedisResource> Cache { get; }

    public Uri WebAppUrl => _webFactory is null
        ? throw new InvalidOperationException("Web app not started.")
        : new Uri(_webFactory.ServerAddress);

    public HttpClient ApiClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _app.StartAsync();

        using var cts = new CancellationTokenSource(ResourceStartupTimeout);

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync(Database.Resource.Name, cts.Token);
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync(Cache.Resource.Name, cts.Token);

        var dbConnectionString = await DatabaseServer.Resource.GetConnectionStringAsync(cts.Token)
                                 ?? throw new InvalidOperationException("Failed to get DB connection string.");

        var builder = new NpgsqlConnectionStringBuilder(dbConnectionString)
        {
            Database = Database.Resource.Name,
            IncludeErrorDetail = true
        };
        _databaseConnectionString = builder.ConnectionString;

        var cacheConnectionString = await Cache.Resource.GetConnectionStringAsync(cts.Token)
                                    ?? throw new InvalidOperationException("Failed to get Redis connection string.");

        _apiFactory = new ApiFactory(_databaseConnectionString, Database.Resource.Name);
        _ = _apiFactory.CreateClient();
        var apiBaseUrl = _apiFactory.ServerAddress;

        _webFactory = new WebFactory(apiBaseUrl, cacheConnectionString);
        _ = _webFactory.CreateClient();

        ApiClient = _apiFactory.CreateClient();

        await Seed(cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_webFactory is not null)
        {
            await _webFactory.DisposeAsync();
        }

        if (_apiFactory is not null)
        {
            await _apiFactory.DisposeAsync();
        }

        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    /// <summary>
    /// Seeds the database with test data. Call at the start of each test.
    /// </summary>
    public async Task Seed(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_apiFactory);

        await using var scope = _apiFactory.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();
        await seeder.Seed(cancellationToken);
    }

    /// <summary>
    /// Clears the database. Call for tests that need a clean slate.
    /// </summary>
    public async Task ClearDatabase(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_apiFactory);

        using var scope = _apiFactory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();
        await seeder.ClearDatabase(cancellationToken);
    }

    private sealed class ApiFactory : WebApplicationFactory<ApiMarker>
    {
        private readonly string _connectionString;
        private readonly string _databaseName;

        public ApiFactory(string connectionString, string databaseName)
        {
            UseKestrel();
            _connectionString = connectionString;
            _databaseName = databaseName;
        }

        public string ServerAddress =>
            ClientOptions.BaseAddress.ToString().TrimEnd('/');

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureWebHost(webBuilder => webBuilder.UseUrls(LoopbackAddress));
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection([
                    new KeyValuePair<string, string?>($"ConnectionStrings:{_databaseName}", _connectionString)
                ]);
            });
            builder.ConfigureServices(services => services.AddSeeding());
            return base.CreateHost(builder);
        }
    }

    private sealed class WebFactory : WebApplicationFactory<WebMarker>
    {
        private readonly string _apiBaseUrl;
        private readonly string _cacheConnectionString;

        public WebFactory(string apiBaseUrl, string cacheConnectionString)
        {
            UseKestrel();
            _apiBaseUrl = apiBaseUrl;
            _cacheConnectionString = cacheConnectionString;
        }

        public string ServerAddress =>
            ClientOptions.BaseAddress.ToString().TrimEnd('/');

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureWebHost(webBuilder => webBuilder.UseUrls(LoopbackAddress));
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddInMemoryCollection([
                    new KeyValuePair<string, string?>("services:api:http:0", _apiBaseUrl),
                    new KeyValuePair<string, string?>("ConnectionStrings:cache", _cacheConnectionString)
                ]);
            });
            return base.CreateHost(builder);
        }
    }
}
