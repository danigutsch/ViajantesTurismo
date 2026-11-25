using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Npgsql;
using SharedKernel.OpenApi;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Catalog.Infrastructure.Media;
using ViajantesTurismo.Catalog.Infrastructure.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Provides extension methods for setting up Catalog infrastructure services.
/// </summary>
public static class InfrastructureDependencyInjection
{
    private const int CatalogSlugLockMaximumPoolSize = 8;
    private const string CatalogDataSourceName = "catalog";
    private const string CatalogLockDataSourceName = "catalog-lock";
    private const string CatalogTransportDataSourceName = "catalog-integration-transport";

    /// <summary>
    /// ponytail: a fixed attempt budget; make it configurable when operations sets a per-environment retry SLO.
    /// </summary>
    private const int CatalogConsumeAttemptLimit = 10;

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
        builder.Services.AddScoped<IPublicContentStore, EfPublicContentStore>();

        var shouldAddOutboxRelay = addOutboxRelay
            ?? !OpenApiGenerationMode.IsEnabled(builder.Environment);

        return builder
            .AddCatalogMediaInfrastructure()
            .AddCatalogTourInfrastructure()
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

    private static TApplicationBuilder AddCatalogPersistence<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDataSource(
            ResourceNames.CatalogDatabase,
            configureDataSourceBuilder: dataSourceBuilder => ConfigureNpgsqlDataSource(dataSourceBuilder, CatalogDataSourceName));
        builder.AddKeyedNpgsqlDataSource(
            ResourceNames.CatalogDatabase,
            configureDataSourceBuilder: dataSourceBuilder =>
            {
                ConfigureNpgsqlDataSource(dataSourceBuilder, CatalogLockDataSourceName);
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

    private static TApplicationBuilder AddCatalogOutbox<TApplicationBuilder>(this TApplicationBuilder builder, bool addOutboxRelay)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddIntegrationEventOutbox<CatalogDbContext>();
        builder.Services.UseDefaultIntegrationEventOutbox<CatalogDbContext>();
        builder.Services.AddIntegrationEventInbox<CatalogDbContext>();
        builder.Services.AddIntegrationEventTransportStorage<CatalogDbContext>();
        if (addOutboxRelay)
        {
            AddCatalogOutboxRouting(builder.Services);
            builder.Services.AddIntegrationEventOutboxRelay<CatalogDbContext>();
            builder.Services.AddPostgreSqlIntegrationEventOutboxRelayAtomicClaims<CatalogDbContext>();
        }

        return builder;
    }

    private static void AddCatalogOutboxRouting(IServiceCollection services)
    {
        var contextKey = typeof(CatalogDbContext);
        services.AddPostgreSqlIntegrationEventTransportProducer<CatalogDbContext>(IntegrationEventConsumerNames.Admin);
        var transportRegistration = services.Last(descriptor =>
            descriptor.ServiceType == typeof(IEventEnvelopePublisher) && Equals(descriptor.ServiceKey, contextKey));
        var transportFactory = transportRegistration.KeyedImplementationFactory
            ?? throw new InvalidOperationException("The Catalog transport publisher must be registered with a keyed factory.");

        services.Remove(transportRegistration);
        services.AddKeyedScoped<IEventEnvelopePublisher>(contextKey, (provider, _) => new CatalogOutboxEnvelopePublisher(
            provider.GetRequiredService<IEventEnvelopePublisher>(),
            (IEventEnvelopePublisher)transportFactory(provider, contextKey)));
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
        options.UseNpgsql(providerOptions => providerOptions.ConfigureDataSource(
            dataSourceBuilder => ConfigureNpgsqlDataSource(dataSourceBuilder, CatalogTransportDataSourceName)));
        ConfigureDevelopmentDatabaseOptions<CatalogIntegrationTransportDbContext, TApplicationBuilder>(builder, options);
    }

    private static void ConfigureNpgsqlDataSource(NpgsqlDataSourceBuilder dataSourceBuilder, string dataSourceName)
    {
        dataSourceBuilder.Name = dataSourceName;
        dataSourceBuilder.ConfigureTracingWithoutFirstResponseEvent();
    }

    /// <summary>
    /// A message that keeps failing is poisoned after the limit so later events keep flowing; operators release it
    /// with IntegrationEventTransportPoison once the cause is fixed.
    /// </summary>
    private static TApplicationBuilder AddCatalogIntegrationEventTransportConsumer<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddPostgreSqlIntegrationEventTransportConsumer<CatalogIntegrationTransportDbContext>(
            IntegrationEventConsumerNames.Catalog,
            options => options.MaxConsumeAttempts = CatalogConsumeAttemptLimit);

        return builder;
    }

    private static void ConfigureDevelopmentDatabaseOptions<TContext, TApplicationBuilder>(
        TApplicationBuilder builder,
        DbContextOptionsBuilder options)
        where TContext : DbContext
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.ApplyDbContextOptionConfigurations<TContext>(options, builder.Environment.IsDevelopment());
    }
}
