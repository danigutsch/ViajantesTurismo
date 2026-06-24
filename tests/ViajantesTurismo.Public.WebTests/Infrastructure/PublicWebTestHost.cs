using PublicWebProgram = Program;
using ViajantesTurismo.Public.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

internal static class PublicWebTestHost
{
    public static WebApplicationFactory<PublicWebProgram> Create(
        string? environment = null,
        FakePublicCatalogApiClient? catalogApiClient = null)
    {
        return new PublicWebApplicationFactory(environment, catalogApiClient ?? new FakePublicCatalogApiClient());
    }

    private sealed class PublicWebApplicationFactory(
        string? environment,
        FakePublicCatalogApiClient catalogApiClient) : WebApplicationFactory<PublicWebProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (environment is not null)
            {
                builder.UseEnvironment(environment);
            }

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPublicCatalogApiClient>();
                services.AddSingleton<IPublicCatalogApiClient>(catalogApiClient);
            });
        }
    }
}
