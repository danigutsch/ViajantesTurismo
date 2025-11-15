using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase<TEntryPoint>(WebApplicationFactory<TEntryPoint> fixture)
    : IAsyncLifetime where TEntryPoint : class
{
    protected HttpClient Client { get; } = fixture.CreateClient();

    public async ValueTask InitializeAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();
        await seeder.Seed(CancellationToken.None);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
