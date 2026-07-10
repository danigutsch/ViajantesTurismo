using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal sealed class BrandingInfrastructureRegistrationScope : IDisposable
{
    private readonly ServiceProvider services;

    private BrandingInfrastructureRegistrationScope(ServiceProvider services)
    {
        this.services = services;
    }

    public static BrandingInfrastructureRegistrationScope Create()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Configuration["ConnectionStrings:catalog-database"] = "Host=localhost;Database=catalog-database;Username=postgres";
        builder.AddBrandingInfrastructure();

        return new BrandingInfrastructureRegistrationScope(builder.Services.BuildServiceProvider());
    }

    public IBrandingSettingsStore GetBrandingSettingsStore()
    {
        return services.GetRequiredService<IBrandingSettingsStore>();
    }

    public void Dispose()
    {
        services.Dispose();
    }
}
