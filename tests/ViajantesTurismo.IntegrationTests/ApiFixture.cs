using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;
using ViajantesTurismo.ApiService;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.IntegrationTests;

/// <summary>
/// Fixture for API integration tests.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<ApiMarker>, IAsyncLifetime
{
    private readonly DistributedApplication _app;
    private string? _databaseConnectionString;

    public ApiFixture()
    {
        var options = new DistributedApplicationOptions() { AssemblyName = typeof(ApiFixture).Assembly.FullName, DisableDashboard = true };
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

        var databaseConnectionString = await DatabaseServer.Resource.GetConnectionStringAsync() ?? throw new InvalidOperationException("Failed to get database connection string.");
        var databaseConnectionStringBuilder = new NpgsqlConnectionStringBuilder(databaseConnectionString)
        {
            Database = Database.Resource.Name
        };

        _databaseConnectionString = databaseConnectionStringBuilder.ConnectionString;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection([
                new KeyValuePair<string, string?>($"ConnectionStrings:{Database.Resource.Name}", _databaseConnectionString)
            ]);
        });

        return base.CreateHost(builder);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _app.StopAsync();
        await _app.DisposeAsync().ConfigureAwait(false);
    }
}