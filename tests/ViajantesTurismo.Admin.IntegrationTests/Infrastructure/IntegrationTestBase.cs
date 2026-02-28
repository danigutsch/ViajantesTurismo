using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase<TEntryPoint>(WebApplicationFactory<TEntryPoint> fixture) where TEntryPoint : class
{
    protected HttpClient Client { get; } = fixture.CreateClient();

    /// <summary>
    /// Clears the database by deleting and recreating it.
    /// Use this for tests that require an empty database.
    /// </summary>
    protected async Task ClearDatabaseAsync(CancellationToken cancellationToken)
    {
        using var scope = fixture.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();
        await seeder.ClearDatabase(cancellationToken);
    }
}
