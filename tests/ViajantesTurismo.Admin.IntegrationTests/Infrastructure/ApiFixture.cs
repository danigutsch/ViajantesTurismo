using Microsoft.AspNetCore.Mvc.Testing;
using ViajantesTurismo.Admin.ApiService;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public sealed class ApiFixture : WebApplicationFactory<ApiMarker>, ITestHost
{
    public HttpClient Client => CreateClient();
    public Uri BaseUri => ClientOptions.BaseAddress ?? new Uri("http://localhost/");

    public async Task Seed()
    {
        // Implement data seeding logic here
        await Task.CompletedTask;
    }

    public async Task Reset()
    {
        // Implement infra cleanup logic here
        await Task.CompletedTask;
    }

    public async ValueTask InitializeAsync() { await Seed(); }

    public override async ValueTask DisposeAsync() { await Reset(); await base.DisposeAsync(); }
}
