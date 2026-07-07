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
using ViajantesTurismo.Catalog.Application.PublicTheme;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Provides extension methods for setting up Catalog infrastructure services.
/// </summary>
public static class InfrastructureDependencyInjection
{
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

        return AddCatalogInfrastructure(builder, addOutboxRelay: true);
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

        builder.AddCatalogIntegrationEventTransportContext();
        builder.Services.AddPostgreSqlIntegrationEventTransportConsumer<CatalogIntegrationTransportDbContext>(IntegrationEventConsumerNames.Catalog);

        return builder;
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

        return builder;
    }

    private static TApplicationBuilder AddCatalogInfrastructure<TApplicationBuilder>(
        TApplicationBuilder builder,
        bool addOutboxRelay)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<CatalogDbContext>(
            ResourceNames.CatalogDatabase,
            configureDbContextOptions: options => ConfigureDevelopmentDatabaseOptions<CatalogDbContext, TApplicationBuilder>(builder, options));

        builder.Services.AddCatalogApplication();
        builder.Services.AddSingleton(TimeProvider.System);
        AddCatalogStores(builder.Services);
        AddCatalogEventSourcing(builder);
        AddCatalogOutbox(builder.Services, addOutboxRelay);

        return builder;
    }

    private static void AddCatalogStores(IServiceCollection services)
    {
        services.AddLocalMediaObjectStorage();
        services.AddScoped<IPublicContentStore, EfPublicContentStore>();
        services.AddScoped<IPublicThemeSettingsStore, EfPublicThemeSettingsStore>();
        services.AddScoped<EfCatalogTourReadModelStore>();
        services.AddScoped<ICatalogTourReadModelStore>(sp => sp.GetRequiredService<EfCatalogTourReadModelStore>());
        services.AddScoped<IPublicMediaImageStore, EfPublicMediaImageStore>();
    }

    private static void AddCatalogEventSourcing<TApplicationBuilder>(TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSingleton(_ =>
        {
            var connectionString = builder.Configuration.GetConnectionString(ResourceNames.CatalogDatabase)
                ?? throw new InvalidOperationException($"Connection string '{ResourceNames.CatalogDatabase}' is not configured.");

            return NpgsqlDataSource.Create(connectionString);
        });
        builder.Services.AddSingleton<IEventSerializer, CatalogEventSerializer>();
        builder.Services.AddSingleton(sp => new PostgreSqlEventStore(
            sp.GetRequiredService<NpgsqlDataSource>(),
            sp.GetRequiredService<IEventSerializer>()));
        builder.Services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<PostgreSqlEventStore>());
        builder.Services.AddSingleton(sp => new PostgreSqlProjectionCheckpointStore(sp.GetRequiredService<NpgsqlDataSource>()));
        builder.Services.AddSingleton<IProjectionCheckpointStore>(sp => sp.GetRequiredService<PostgreSqlProjectionCheckpointStore>());
        builder.Services.AddScoped<IProjection, CatalogTourReadModelProjection>();
        builder.Services.AddScoped<CatalogProjectionRunner>();
    }

    private static void AddCatalogOutbox(IServiceCollection services, bool addOutboxRelay)
    {
        services.AddIntegrationEventOutbox<CatalogDbContext>();
        if (addOutboxRelay)
        {
            services.AddIntegrationEventOutboxRelay<CatalogDbContext>();
            services.AddPostgreSqlIntegrationEventOutboxRelayAtomicClaims<CatalogDbContext>();
        }
    }

    private static void AddCatalogIntegrationEventTransportContext<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<CatalogIntegrationTransportDbContext>(
            ResourceNames.AdminDatabase,
            configureDbContextOptions: options => ConfigureDevelopmentDatabaseOptions<CatalogIntegrationTransportDbContext, TApplicationBuilder>(builder, options));
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
