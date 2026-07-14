using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.DomainEvents.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Provides extension methods for setting up the Infrastructure layer services in the application.
/// </summary>
public static class InfrastructureDependencyInjection
{
    private const string OpenApiDocumentGeneratorAssemblyName = "GetDocument.Insider";
    private const string OpenApiBuildGenerationConfigurationKey = "OpenApi:BuildGeneration";

    /// <summary>
    /// Adds the Infrastructure layer services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return AddInfrastructure(
            builder,
            addRuntimeBackgroundServices: !IsOpenApiBuildGeneration(builder.Configuration));
    }

    /// <summary>
    /// Adds the seeding services to the application builder, including the database context and seeder implementation.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddAdminSeeding<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDbContextDevelopmentDiagnostics<AdminWriteDbContext>();
        }

        builder.AddAdminWriteDbContext();
        builder.Services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent);
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        builder.Services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        builder.Services.AddScoped(sp => new Seeder(sp.GetRequiredService<AdminWriteDbContext>()));

        return builder;
    }

    private static TApplicationBuilder AddInfrastructure<TApplicationBuilder>(
        TApplicationBuilder builder,
        bool addRuntimeBackgroundServices)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDbContextDevelopmentDiagnostics<AdminWriteDbContext>();
            builder.Services.AddDbContextDevelopmentDiagnostics<AdminReadDbContext>();
        }

        builder.AddAdminWriteDbContext();
        builder.AddAdminReadDbContext();

        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AdminWriteDbContext>());
        builder.Services.AddScoped<IQueryService, QueryService>();
        builder.Services.AddScoped<ITourStore, TourStore>();
        builder.Services.AddScoped<ICustomerStore, CustomerStore>();
        builder.Services.AddScoped<IDocumentStore, DocumentStore>();
        builder.Services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent);
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        builder.Services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);

        return builder.AddAdminRuntimeBackgroundServices(addRuntimeBackgroundServices);
    }

    private static TApplicationBuilder AddAdminRuntimeBackgroundServices<TApplicationBuilder>(
        this TApplicationBuilder builder,
        bool addRuntimeBackgroundServices)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        if (addRuntimeBackgroundServices)
        {
            builder.Services.AddHostedService<DocumentDraftRetentionHostedService>();
            builder.Services.AddIntegrationEventOutboxRelay<AdminWriteDbContext>();
            builder.Services.AddPostgreSqlIntegrationEventOutboxRelayAtomicClaims<AdminWriteDbContext>();
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

    private static void AddAdminWriteDbContext<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.AddDomainEventDispatch<AdminWriteDbContext>();

        builder.AddNpgsqlDbContext<AdminWriteDbContext>(
            ResourceNames.AdminDatabase,
            configureDbContextOptions: options => ConfigureAdminWriteDbContext(builder, options));
    }

    private static void AddAdminReadDbContext<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<AdminReadDbContext>(
            ResourceNames.AdminDatabase,
            configureDbContextOptions: options => ConfigureReadDatabaseOptions(builder, options));
    }

    private static void ConfigureReadDatabaseOptions<TApplicationBuilder>(
        TApplicationBuilder builder,
        DbContextOptionsBuilder options)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        builder.Services.ApplyDbContextOptionConfigurations<AdminReadDbContext>(options);
    }

    private static void ConfigureAdminWriteDbContext<TApplicationBuilder>(
        TApplicationBuilder builder,
        DbContextOptionsBuilder options)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
    }

}
