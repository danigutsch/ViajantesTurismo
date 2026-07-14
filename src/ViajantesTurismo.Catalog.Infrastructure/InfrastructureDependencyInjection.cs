using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.EventSourcing;
using SharedKernel.EventSourcing.Npgsql;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Projections;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Provides extension methods for setting up Catalog infrastructure services.
/// </summary>
public static class InfrastructureDependencyInjection
{
    private const string OpenApiDocumentGeneratorAssemblyName = "GetDocument.Insider";
    private const string OpenApiBuildGenerationConfigurationKey = "OpenApi:BuildGeneration";

    /// <summary>
    /// Adds Catalog infrastructure services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddCatalogInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var isOpenApiBuildGeneration = IsOpenApiBuildGeneration(builder.Configuration);

        return AddCatalogInfrastructure(builder, addOutboxRelay: !isOpenApiBuildGeneration);
    }

    /// <summary>
    /// Adds Catalog infrastructure required by the migration service before schemas are fully migrated.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddCatalogSeeding<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return AddCatalogInfrastructure(builder, addOutboxRelay: false);
    }

    /// <summary>
    /// Adds Catalog integration-event transport consumption for API-hosted delivery mode.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddCatalogHostedIntegrationEventTransport<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddCatalogIntegrationEventTransportContext()
            .AddCatalogIntegrationEventTransportConsumer();
    }

    /// <summary>
    /// Adds Catalog infrastructure needed by the standalone integration-event worker.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddCatalogIntegrationEventWorkerInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddCatalogInfrastructure(builder, addOutboxRelay: false);
        builder.AddCatalogHostedIntegrationEventTransport();
        builder.Services.AddHostedService<CatalogProjectionHostedService>();
        builder.Services.AddHostedService<MediaObjectReconciliationHostedService>();

        return builder;
    }

    private static TApplicationBuilder AddCatalogInfrastructure<TApplicationBuilder>(
        TApplicationBuilder builder,
        bool addOutboxRelay)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDataSource(ResourceNames.CatalogDatabase);
        builder.Services.AddDbContextPool<CatalogDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>());
            ConfigureDevelopmentDatabaseOptions<CatalogDbContext, TApplicationBuilder>(builder, options);
        });

        builder.Services.AddCatalogApplication();
        builder.AddCatalogAiTextGeneration();
        builder.Services.AddSingleton(TimeProvider.System);

        return builder
            .AddCatalogStoreInfrastructure()
            .AddCatalogEventStore()
            .AddCatalogOutbox(addOutboxRelay);
    }

    private static TApplicationBuilder AddCatalogStoreInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        if (builder.Configuration.GetSection(SeaweedFsMediaObjectStorageOptions.SectionName).Exists())
        {
            builder.Services.AddSeaweedFsMediaObjectStorage();
        }
        else
        {
            builder.Services.AddLocalMediaObjectStorage();
        }
        if (builder.Configuration.GetSection(ClamAvMediaUploadScannerOptions.SectionName).Exists())
        {
            builder.Services.AddClamAvMediaUploadScanner();
        }
        else if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<IMediaUploadScanner, NoOpMediaUploadScanner>();
        }
        else
        {
            builder.Services.AddClamAvMediaUploadScanner();
        }
        builder.Services.AddScoped<IPublicContentStore, EfPublicContentStore>();
        builder.Services.AddScoped<ICatalogTourReadModelStore, EfCatalogTourReadModelStore>();
        builder.Services.AddScoped<IPublicMediaImageStore, EfPublicMediaImageStore>();

        return builder;
    }

    private static TApplicationBuilder AddCatalogEventStore<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSingleton<IEventSerializer, CatalogEventSerializer>();
        builder.Services.AddSingleton<IEventStore, PostgreSqlEventStore>();
        builder.Services.AddSingleton<IProjectionCheckpointStore, PostgreSqlProjectionCheckpointStore>();
        builder.Services.AddScoped<IProjection, CatalogTourReadModelProjection>();
        builder.Services.AddScoped<CatalogProjectionRunner>();

        return builder;
    }

    private static TApplicationBuilder AddCatalogOutbox<TApplicationBuilder>(this TApplicationBuilder builder, bool addOutboxRelay)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddIntegrationEventOutbox<CatalogDbContext>();
        if (addOutboxRelay)
        {
            builder.Services.AddIntegrationEventOutboxRelay<CatalogDbContext>();
            builder.Services.AddPostgreSqlIntegrationEventOutboxRelayAtomicClaims<CatalogDbContext>();
        }

        return builder;
    }

    private static bool IsOpenApiBuildGeneration(IConfiguration configuration)
    {
        return bool.TryParse(configuration[OpenApiBuildGenerationConfigurationKey], out var enabled)
               && enabled
               && string.Equals(
                   Assembly.GetEntryAssembly()?.GetName().Name,
                   OpenApiDocumentGeneratorAssemblyName,
                   StringComparison.Ordinal);
    }

    private static TApplicationBuilder AddCatalogIntegrationEventTransportContext<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<CatalogIntegrationTransportDbContext>(
            ResourceNames.AdminDatabase,
            configureDbContextOptions: options => ConfigureDevelopmentDatabaseOptions<CatalogIntegrationTransportDbContext, TApplicationBuilder>(builder, options));

        return builder;
    }

    private static TApplicationBuilder AddCatalogIntegrationEventTransportConsumer<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddPostgreSqlIntegrationEventTransportConsumer<CatalogIntegrationTransportDbContext>(IntegrationEventConsumerNames.Catalog);

        return builder;
    }

    private static void ConfigureDevelopmentDatabaseOptions<TContext, TApplicationBuilder>(
        TApplicationBuilder builder,
        DbContextOptionsBuilder options)
        where TContext : DbContext
        where TApplicationBuilder : IHostApplicationBuilder
    {
        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.ApplyDbContextOptionConfigurations<TContext>(options);
            return;
        }

        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
        builder.Services.ApplyDbContextOptionConfigurations<TContext>(options);
    }
}
