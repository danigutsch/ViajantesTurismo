using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PandaBear.Messaging;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>Builds the Branding infrastructure registrations in a host so tests can inspect them.</summary>
internal sealed class BrandingInfrastructureRegistrationScope : IDisposable
{
    private readonly ServiceProvider services;

    private BrandingInfrastructureRegistrationScope(ServiceProvider services)
    {
        this.services = services;
    }

    /// <summary>Creates a scope for the given host environment, optionally registering the outbox relay.</summary>
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

    /// <summary>Resolves the Branding settings store.</summary>
    public IBrandingSettingsStore GetBrandingSettingsStore()
    {
        return services.GetRequiredService<IBrandingSettingsStore>();
    }

    /// <summary>Returns whether EF sensitive data logging is enabled for the Branding context.</summary>
    public bool IsSensitiveDataLoggingEnabled()
    {
        var options = services.GetRequiredService<DbContextOptions<BrandingDbContext>>();
        return options.FindExtension<CoreOptionsExtension>()?.IsSensitiveDataLoggingEnabled ?? false;
    }

    /// <summary>Returns whether the named activity source produces activities under the configured tracer.</summary>
    public bool IsActivitySourceEnabled(string sourceName)
    {
        _ = services.GetRequiredService<TracerProvider>();
        using var activitySource = new ActivitySource(sourceName);
        using var activity = activitySource.StartActivity("privacy-test", ActivityKind.Client);
        return activity is not null;
    }

    /// <summary>Returns whether a meter provider is registered.</summary>
    public bool HasMeterProvider()
    {
        return services.GetService<MeterProvider>() is not null;
    }

    /// <summary>Reports which outbox, transport and serializer registrations exist for the Branding context, with their table schemas.</summary>
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

    /// <summary>Disposes the underlying service provider.</summary>
    public void Dispose()
    {
        services.Dispose();
    }
}
