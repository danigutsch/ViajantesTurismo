using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal sealed class BrandingInfrastructureRegistrationScope : IDisposable
{
    private readonly ServiceProvider services;

    private BrandingInfrastructureRegistrationScope(ServiceProvider services)
    {
        this.services = services;
    }

    public static BrandingInfrastructureRegistrationScope Create(
        string environmentName = "Production",
        bool addOutboxRelay = true)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = environmentName
        });
        builder.Configuration["ConnectionStrings:catalog-database"] = "Host=localhost;Database=catalog-database;Username=postgres";
        builder.Services.AddOpenTelemetry().WithTracing(static _ => { });
        builder.AddBrandingInfrastructure(addOutboxRelay);

        return new BrandingInfrastructureRegistrationScope(builder.Services.BuildServiceProvider());
    }

    public IBrandingSettingsStore GetBrandingSettingsStore()
    {
        return services.GetRequiredService<IBrandingSettingsStore>();
    }

    public bool IsSensitiveDataLoggingEnabled()
    {
        var options = services.GetRequiredService<DbContextOptions<BrandingDbContext>>();
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

    public (
        bool HasOutbox,
        bool HasTransportPublisher,
        bool HasSerializer,
        int OutboxRelayCount,
        string? OutboxSchema,
        string? TransportSchema,
        bool TransportExcludedFromMigrations) GetMessagingRegistrations()
    {
        using var scope = services.CreateScope();
        var hasOutbox = scope.ServiceProvider.GetKeyedService<IIntegrationEventOutbox>(typeof(BrandingDbContext)) is not null;
        var hasTransportPublisher = scope.ServiceProvider.GetKeyedService<IEventEnvelopePublisher>(typeof(BrandingDbContext)) is not null;
        var hasSerializer = scope.ServiceProvider.GetKeyedService<IIntegrationEventSerializer>(typeof(BrandingDbContext)) is not null;
        var relayCount = services.GetServices<IHostedService>()
            .Count(static service =>
                service.GetType().IsGenericType
                && service.GetType().GetGenericArguments().Contains(typeof(BrandingDbContext))
                && service.GetType().Name.StartsWith("IntegrationEventOutboxRelayHostedService", StringComparison.Ordinal));
        var dbContext = scope.ServiceProvider.GetRequiredService<BrandingDbContext>();
        var model = dbContext.GetService<IDesignTimeModel>().Model;
        var outbox = model.GetEntityTypes().SingleOrDefault(
            entity => entity.GetTableName()?.Equals("outbox_messages", StringComparison.Ordinal) == true);
        var transport = model.GetEntityTypes().SingleOrDefault(
            entity => entity.GetTableName()?.Equals("transport_messages", StringComparison.Ordinal) == true);

        return (
            hasOutbox,
            hasTransportPublisher,
            hasSerializer,
            relayCount,
            outbox?.GetSchema(),
            transport?.GetSchema(),
            transport?.IsTableExcludedFromMigrations() ?? false);
    }

    public void Dispose()
    {
        services.Dispose();
    }
}
