using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal sealed class BrandingInfrastructureRegistrationScope : IDisposable
{
    private readonly ServiceProvider services;

    private BrandingInfrastructureRegistrationScope(ServiceProvider services)
    {
        this.services = services;
    }

    public static BrandingInfrastructureRegistrationScope Create(string environmentName = "Production")
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environmentName
        });
        builder.Configuration["ConnectionStrings:catalog-database"] = "Host=localhost;Database=catalog-database;Username=postgres";
        builder.Services.AddOpenTelemetry().WithTracing(static _ => { });
        builder.AddBrandingInfrastructure();

        return new BrandingInfrastructureRegistrationScope(builder.Services.BuildServiceProvider());
    }

    public IBrandingSettingsStore GetBrandingSettingsStore()
    {
        return services.GetRequiredService<IBrandingSettingsStore>();
    }

    public bool IsSensitiveDataLoggingEnabled()
    {
        var options = services.GetRequiredService<Microsoft.EntityFrameworkCore.DbContextOptions<BrandingDbContext>>();
        return options.FindExtension<CoreOptionsExtension>()?.IsSensitiveDataLoggingEnabled ?? false;
    }

    public bool IsActivitySourceEnabled(string sourceName)
    {
        _ = services.GetRequiredService<TracerProvider>();
        using var activitySource = new ActivitySource(sourceName);
        using var activity = activitySource.StartActivity("privacy-test", ActivityKind.Client);
        return activity is not null;
    }

    public bool HasMeterProvider()
    {
        return services.GetService<MeterProvider>() is not null;
    }

    public void Dispose()
    {
        services.Dispose();
    }
}
