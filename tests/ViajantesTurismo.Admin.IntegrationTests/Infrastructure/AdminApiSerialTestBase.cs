using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that require sequential execution with a clean database.
/// Seeds before each test and clears after. Use for tests that call ClearDatabaseAsync
/// mid-test or assert exact global counts.
/// </summary>
[Collection("Integration.Serial")]
public abstract class AdminApiSerialTestBase(ApiFixture fixture) : IntegrationTestBase<ApiMarker>(fixture), IAsyncLifetime
{
    private static readonly TimeSpan DatabaseResetTimeout = TimeSpan.FromSeconds(30);

    public async ValueTask InitializeAsync()
    {
        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await ClearDatabaseAsync(cts.Token);

        using var scope = fixture.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();
        await seeder.Seed(cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await ClearDatabaseAsync(cts.Token);

        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition("Integration.Serial", DisableParallelization = true)]
public sealed class IntegrationSerialTests;
