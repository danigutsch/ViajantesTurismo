using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

/// <summary>
/// Fixture for API integration tests.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<ApiMarker>, IAsyncLifetime
{
    private const string LoopbackAddress = "http://127.0.0.1:0";
    private static readonly TimeSpan ResourceStartupTimeout = TimeSpan.FromSeconds(30);

    private readonly DistributedApplication _app;
    private string? _databaseConnectionString;

    public ApiFixture()
    {
        UseKestrel();

        var options = new DistributedApplicationOptions { AssemblyName = typeof(ApiFixture).Assembly.FullName, DisableDashboard = true };
        var appBuilder = new DistributedApplicationBuilder(options);
        DatabaseServer = appBuilder.AddPostgres(ResourceNames.DatabaseServer);
        Database = DatabaseServer.AddDatabase(ResourceNames.Database);

        _app = appBuilder.Build();
    }

    public IResourceBuilder<PostgresServerResource> DatabaseServer { get; }
    public IResourceBuilder<PostgresDatabaseResource> Database { get; }

    public async ValueTask InitializeAsync()
    {
        await _app.StartAsync();

        using var cts = new CancellationTokenSource(ResourceStartupTimeout);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(Database.Resource.Name, cts.Token);

        var databaseConnectionString = await DatabaseServer.Resource.GetConnectionStringAsync(cts.Token) ?? throw new InvalidOperationException("Failed to get database connection string.");
        var databaseConnectionStringBuilder = new NpgsqlConnectionStringBuilder(databaseConnectionString)
        {
            Database = Database.Resource.Name,
            IncludeErrorDetail = true
        };

        _databaseConnectionString = databaseConnectionStringBuilder.ConnectionString;

        // Trigger host creation and seed once for the assembly
        _ = CreateClient();
        using var seedScope = Services.CreateScope();
        var seeder = seedScope.ServiceProvider.GetRequiredService<ISeeder>();
        await seeder.Seed(cts.Token);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureWebHost(webBuilder => webBuilder.UseUrls(LoopbackAddress));

        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection([
                new KeyValuePair<string, string?>($"ConnectionStrings:{Database.Resource.Name}", _databaseConnectionString)
            ]);
        });

        builder.ConfigureServices(services => { services.AddSeeding(); });

        return base.CreateHost(builder);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
