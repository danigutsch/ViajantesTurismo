using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SharedKernel.AI;
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
        builder.Services.AddCatalogAiTextGeneration(builder.Configuration);
        builder.Services.AddSingleton(TimeProvider.System);

        return builder
            .AddCatalogStoreInfrastructure()
            .AddCatalogEventStore()
            .AddCatalogOutbox(addOutboxRelay);
    }

    private static TApplicationBuilder AddCatalogStoreInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddLocalMediaObjectStorage();
        builder.Services.AddScoped<IPublicContentStore, EfPublicContentStore>();
        builder.Services.AddScoped<IPublicThemeSettingsStore, EfPublicThemeSettingsStore>();
        builder.Services.AddScoped<ICatalogTourReadModelStore, EfCatalogTourReadModelStore>();
        builder.Services.AddScoped<IPublicMediaImageStore, EfPublicMediaImageStore>();

        return builder;
    }

    private static void AddCatalogAiTextGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(LiteLlmImageTextGeneratorOptions.SectionName);
        var endpointValue = section[nameof(LiteLlmImageTextGeneratorOptions.Endpoint)];
        Uri? endpoint = null;
        if (!string.IsNullOrWhiteSpace(endpointValue))
        {
            Uri.TryCreate(endpointValue, UriKind.Absolute, out endpoint);
        }

        var options = new LiteLlmImageTextGeneratorOptions
        {
            Endpoint = endpoint,
            ApiKey = section[nameof(LiteLlmImageTextGeneratorOptions.ApiKey)],
            Model = section[nameof(LiteLlmImageTextGeneratorOptions.Model)],
            ChatCompletionsPath = section[nameof(LiteLlmImageTextGeneratorOptions.ChatCompletionsPath)] ?? "/v1/chat/completions"
        };

        services.AddSingleton(options);
        services.AddHttpClient<IImageTextGenerator, LiteLlmImageTextGenerator>((serviceProvider, client) =>
        {
            var configuredOptions = serviceProvider.GetRequiredService<LiteLlmImageTextGeneratorOptions>();
            if (configuredOptions.Endpoint is not null)
            {
                client.BaseAddress = configuredOptions.Endpoint;
            }
        });
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
