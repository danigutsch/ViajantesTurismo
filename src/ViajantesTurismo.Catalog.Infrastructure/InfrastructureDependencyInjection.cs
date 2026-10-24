using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using PandaBear.EntityFrameworkCore;
using PandaBear.EventSourcing;
using PandaBear.EventSourcing.Npgsql;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Messaging.IntegrationEvents;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using PandaBear.Npgsql;
using PandaBear.OpenApi;
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
    private const int CatalogSlugLockMaximumPoolSize = 8;

    /// <summary>
    /// Adds Catalog infrastructure services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddCatalogInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        return builder.AddCatalogInfrastructure(addOutboxRelay: null);
    }

    /// <summary>
    /// Adds Catalog infrastructure services with an explicit outbox-relay registration choice.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <param name="addOutboxRelay">Whether to register runtime outbox relay services. When omitted, trusted OpenAPI generation omits them.</param>
    /// <typeparam name="TApplicationBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddCatalogInfrastructure<TApplicationBuilder>(
        this TApplicationBuilder builder,
        bool? addOutboxRelay)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddCatalogPersistence();
        builder.Services.AddCatalogApplication();
        builder.AddCatalogAiTextGeneration();
        builder.Services.AddSingleton(TimeProvider.System);

        var shouldAddOutboxRelay = addOutboxRelay
            ?? !OpenApiGenerationMode.IsEnabled(builder.Environment);

        return builder
            .AddCatalogStoreInfrastructure()
            .AddCatalogEventStore()
            .AddCatalogOutbox(shouldAddOutboxRelay);
    }

    /// <summary>
    /// Adds Catalog infrastructure required by the migration service before schemas are fully migrated.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddCatalogDatabaseInitialization<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddCatalogPersistence()
            .AddCatalogOutbox(addOutboxRelay: false);
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

    private static TApplicationBuilder AddCatalogStoreInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        if (builder.Configuration.GetSection(SeaweedFsMediaObjectStorageOptions.S3SectionName).Exists())
        {
            builder.Services.AddSeaweedFsMediaObjectStorage(SeaweedFsMediaObjectStorageOptions.S3SectionName);
        }
        else if (builder.Configuration.GetSection(SeaweedFsMediaObjectStorageOptions.SectionName).Exists())
        {
            builder.Services.AddSeaweedFsMediaObjectStorage();
        }
        else if (builder.Environment.IsDevelopment() || OpenApiGenerationMode.IsEnabled(builder.Environment))
        {
            builder.Services.AddLocalMediaObjectStorage();
        }
        else
        {
            throw new InvalidOperationException(
                $"Catalog durable media object storage requires configuration section '{SeaweedFsMediaObjectStorageOptions.SectionName}'.");
        }
        builder.Services.AddConfiguredClamAvMalwareScanner(builder.Configuration, builder.Environment);
        builder.Services.AddSingleton<IMediaUploadScanner, MalwareScannerMediaUploadScanner>();
        builder.Services.AddScoped<IPublicContentStore, EfPublicContentStore>();
        builder.Services.AddScoped<ICatalogTourReadModelStore, EfCatalogTourReadModelStore>();
        builder.Services.AddScoped<IPublicMediaImageStore, EfPublicMediaImageStore>();

        return builder;
    }

    private static TApplicationBuilder AddCatalogPersistence<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDataSource(
            ResourceNames.CatalogDatabase,
            configureDataSourceBuilder: ConfigureNpgsqlDataSource);
        builder.AddKeyedNpgsqlDataSource(
            ResourceNames.CatalogDatabase,
            configureDataSourceBuilder: dataSourceBuilder =>
            {
                ConfigureNpgsqlDataSource(dataSourceBuilder);
                dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = Math.Min(
                    CatalogSlugLockMaximumPoolSize,
                    dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize);
            });
        builder.Services.AddSingleton<ICatalogTourSlugLock>(serviceProvider =>
            new PostgreSqlCatalogTourSlugLock(
                serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(ResourceNames.CatalogDatabase)));
        builder.Services.AddSingleton<ICatalogTourMutationLock>(serviceProvider =>
            new PostgreSqlCatalogTourMutationLock(
                serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(ResourceNames.CatalogDatabase)));
        builder.Services.AddDbContextPool<CatalogDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>());
            ConfigureDevelopmentDatabaseOptions<CatalogDbContext, TApplicationBuilder>(builder, options);
        });

        return builder;
    }

    private static TApplicationBuilder AddCatalogEventStore<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSingleton<IEventSerializer, CatalogEventSerializer>();
        builder.Services.AddSingleton<IEventStore, PostgreSqlEventStore>();
        builder.Services.AddSingleton<IProjectionCheckpointStore, PostgreSqlProjectionCheckpointStore>();
        builder.Services.AddScoped<CatalogTourReadModelProjection>();
        builder.Services.AddScoped<IProjection>(services => services.GetRequiredService<CatalogTourReadModelProjection>());
        builder.Services.AddScoped<CatalogProjectionRunner>();

        return builder;
    }

    // A co-hosted module such as Branding also registers an unkeyed outbox, and the first registration
    // would win. Catalog code resolves the unkeyed outbox, so the Catalog host claims it explicitly;
    // co-hosted modules resolve their own outbox by context key.
    private static void UseCatalogOutboxAsHostDefault(IServiceCollection services)
    {
        foreach (var descriptor in services
                     .Where(descriptor => descriptor.ServiceType == typeof(IIntegrationEventOutbox) && !descriptor.IsKeyedService)
                     .ToArray())
        {
            services.Remove(descriptor);
        }

        services.AddScoped(serviceProvider =>
            serviceProvider.GetRequiredKeyedService<IIntegrationEventOutbox>(typeof(CatalogDbContext)));
    }

    private static TApplicationBuilder AddCatalogOutbox<TApplicationBuilder>(this TApplicationBuilder builder, bool addOutboxRelay)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddIntegrationEventOutbox<CatalogDbContext>();
        UseCatalogOutboxAsHostDefault(builder.Services);
        builder.Services.AddIntegrationEventInbox<CatalogDbContext>();
        builder.Services.AddIntegrationEventTransportStorage<CatalogDbContext>();
        if (addOutboxRelay)
        {
            builder.Services.AddIntegrationEventOutboxRelay<CatalogDbContext>();
            builder.Services.AddPostgreSqlIntegrationEventOutboxRelayAtomicClaims<CatalogDbContext>();
        }

        return builder;
    }

    private static TApplicationBuilder AddCatalogIntegrationEventTransportContext<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<CatalogIntegrationTransportDbContext>(
            ResourceNames.AdminDatabase,
            configureDbContextOptions: options => ConfigureCatalogIntegrationTransportDbContext(builder, options));

        return builder;
    }

    private static void ConfigureCatalogIntegrationTransportDbContext<TApplicationBuilder>(
        TApplicationBuilder builder,
        DbContextOptionsBuilder options)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        options.UseNpgsql(providerOptions => providerOptions.ConfigureDataSource(ConfigureNpgsqlDataSource));
        ConfigureDevelopmentDatabaseOptions<CatalogIntegrationTransportDbContext, TApplicationBuilder>(builder, options);
    }

    private static void ConfigureNpgsqlDataSource(NpgsqlDataSourceBuilder dataSourceBuilder)
    {
        dataSourceBuilder.ConfigureTracingWithoutFirstResponseEvent();
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
