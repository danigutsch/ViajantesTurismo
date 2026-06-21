using PublicWebProgram = Program;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

internal static class PublicWebTestHost
{
    public static WebApplicationFactory<PublicWebProgram> Create(string? environment = null)
    {
        var factory = new WebApplicationFactory<PublicWebProgram>();
        return environment is null
            ? factory
            : factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
    }
}
