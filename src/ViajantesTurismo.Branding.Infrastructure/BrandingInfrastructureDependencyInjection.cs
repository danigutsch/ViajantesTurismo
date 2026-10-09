using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SharedKernel.Branding;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Npgsql;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>
/// Provides extension methods for setting up Branding infrastructure services.
/// </summary>
public static class BrandingInfrastructureDependencyInjection
{
    private const string MigrationsHistoryTable = "__EFMigrationsHistory_Branding";

    /// <summary>
    /// Adds Branding infrastructure services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TBuilder AddBrandingInfrastructure<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        return builder.AddBrandingInfrastructure(addOutboxRelay: true);
    }

    /// <summary>
    /// Adds Branding infrastructure services with an explicit outbox-relay registration choice.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <param name="addOutboxRelay">Whether to register the runtime outbox relay.</param>
    /// <typeparam name="TBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TBuilder AddBrandingInfrastructure<TBuilder>(
        this TBuilder builder,
        bool addOutboxRelay)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDbContext<BrandingDbContext>(
            ResourceNames.CatalogDatabase,
            configureDbContextOptions: options => ConfigureBrandingDatabaseOptions<BrandingDbContext, TBuilder>(builder, options));

        builder.Services.AddScoped<IBrandingSettingsStore, EfBrandingSettingsStore>();
        builder.Services.AddKeyedSingleton<IIntegrationEventSerializer, BrandingIntegrationEventSerializer>(typeof(BrandingDbContext));
        builder.Services.ConfigureIntegrationEventStorage<BrandingDbContext>(ConfigureBrandingIntegrationEventStorage);
        builder.Services.AddIntegrationEventOutbox<BrandingDbContext>();
        builder.Services.AddPostgreSqlIntegrationEventTransportProducer<BrandingDbContext>(IntegrationEventConsumerNames.Admin);
        if (addOutboxRelay)
        {
            builder.Services.AddIntegrationEventOutboxRelay<BrandingDbContext>();
            builder.Services.AddPostgreSqlIntegrationEventOutboxRelayAtomicClaims<BrandingDbContext>();
        }

        return builder;
    }

    internal static void ConfigureBrandingIntegrationEventStorage(IntegrationEventStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Schema = SharedKernelSchemas.Messaging;
        options.OutboxSchema = BrandingDbContext.SchemaName;
        options.TransportSchema = SharedKernelSchemas.Messaging;
        options.ExcludeTransportFromMigrations = true;
    }

    private static void ConfigureBrandingDatabaseOptions<TContext, TBuilder>(
        TBuilder builder,
        DbContextOptionsBuilder options)
        where TContext : DbContext
        where TBuilder : IHostApplicationBuilder
    {
        options.UseNpgsql(providerOptions =>
        {
            providerOptions.MigrationsHistoryTable(
                MigrationsHistoryTable,
                schema: BrandingDbContext.MigrationsHistorySchemaName);
            providerOptions.ConfigureDataSource(ConfigureNpgsqlDataSource);
        });

        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.ApplyDbContextOptionConfigurations<TContext>(options);
            return;
        }

        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
        builder.Services.ApplyDbContextOptionConfigurations<TContext>(options);
    }

    private static void ConfigureNpgsqlDataSource(NpgsqlDataSourceBuilder dataSourceBuilder)
    {
        dataSourceBuilder.ConfigureTracingWithoutFirstResponseEvent();
    }
}
