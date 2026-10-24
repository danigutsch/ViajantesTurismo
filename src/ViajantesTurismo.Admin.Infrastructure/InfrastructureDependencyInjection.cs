using System.Text.Json.Serialization.Metadata;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using PandaBear.AuditTrail;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using PandaBear.Npgsql;
using PandaBear.OpenApi;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Branding;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Provides extension methods for setting up the Infrastructure layer services in the application.
/// </summary>
public static class InfrastructureDependencyInjection
{
    private const int TourCapacityLockMaximumPoolSize = 8;

    /// <summary>
    /// Applies Admin migrations from the dedicated database initialization application.
    /// </summary>
    /// <param name="serviceProvider">The scoped service provider containing the Admin write context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the migration operation.</returns>
    public static async Task MigrateAdminDatabase(this IServiceProvider serviceProvider, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var dbContext = serviceProvider.GetRequiredService<AdminWriteDbContext>();
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync(ct);
        }
    }

    /// <summary>
    /// Adds the Infrastructure layer services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        return builder.AddInfrastructure(addRuntimeBackgroundServices: null);
    }

    /// <summary>
    /// Adds Infrastructure layer services with an explicit runtime background-service registration choice.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <param name="addRuntimeBackgroundServices">Whether to register runtime hosted services. When omitted, trusted OpenAPI generation omits them.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddInfrastructure<TApplicationBuilder>(
        this TApplicationBuilder builder,
        bool? addRuntimeBackgroundServices)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDbContextDevelopmentDiagnostics<AdminWriteDbContext>();
            builder.Services.AddDbContextDevelopmentDiagnostics<AdminReadDbContext>();
        }

        builder.AddTourCapacityMutationLock();
        builder.AddAdminWriteDbContext();
        builder.AddAdminReadDbContext();

        builder.Services.AddSingleton<PaymentProtection>();
        builder.Services.AddHostedService<PaymentProtectionStartupCheck>();
        builder.Services.AddScoped<IPaymentTransaction, PaymentTransaction>();
        builder.Services.AddSingleton(sp =>
        {
            var enabled = bool.TryParse(builder.Configuration["Payments:Enabled"], out var configured) && configured;
            var zone = builder.Configuration["Payments:BusinessTimeZone"];
            if (enabled && string.IsNullOrWhiteSpace(zone))
            {
                throw new InvalidOperationException("Payments require an explicit business time zone.");
            }
            return new PaymentBusinessCalendar(sp.GetRequiredService<TimeProvider>(), TimeZoneInfo.FindSystemTimeZoneById(zone ?? "UTC"));
        });
        builder.Services.AddScoped<PaymentHistoryStore>();
        builder.Services.AddScoped<IPaymentHistoryStore>(sp => sp.GetRequiredService<PaymentHistoryStore>());
        builder.Services.AddScoped<IPaymentAccountStore, PaymentAccountStore>();
        builder.Services.AddScoped<IPaymentAccountQueryService, PaymentAccountQueryService>();
        builder.Services.AddScoped<IPaymentFinancialQueryService, PaymentFinancialQueryService>();
        builder.Services.AddScoped<IPaymentInstructionStore, PaymentInstructionStore>();
        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AdminWriteDbContext>());
        builder.Services.AddScoped<IQueryService, QueryService>();
        builder.Services.AddScoped<IDocumentQueryService, DocumentQueryService>();
        builder.Services.AddScoped<ITourStore, TourStore>();
        builder.Services.AddScoped<ICustomerStore, CustomerStore>();
        builder.Services.AddScoped<IDocumentStore, DocumentStore>();
        builder.Services.AddScoped<IDocumentAuditStore, DocumentAuditStore>();
        builder.Services.AddScoped<IBrandingProjectionStore, EfBrandingProjectionStore>();
        JsonTypeInfo<AdminTourCreatedIntegrationEvent> adminTourCreatedJsonTypeInfo =
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent;
        builder.Services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            adminTourCreatedJsonTypeInfo);
        builder.Services.AddDomainEventProcessing();
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        builder.Services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);

        var shouldAddRuntimeBackgroundServices = addRuntimeBackgroundServices
            ?? !OpenApiGenerationMode.IsEnabled(builder.Environment);

        return builder.AddAdminRuntimeBackgroundServices(shouldAddRuntimeBackgroundServices);
    }

    /// <summary>
    /// Adds Admin persistence services used by the dedicated database initialization application.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddAdminDatabaseInitialization<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDbContextDevelopmentDiagnostics<AdminWriteDbContext>();
        }

        builder.AddAdminWriteDbContext();
        JsonTypeInfo<AdminTourCreatedIntegrationEvent> adminTourCreatedJsonTypeInfo =
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent;
        builder.Services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            adminTourCreatedJsonTypeInfo);
        builder.Services.AddDomainEventProcessing();
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        builder.Services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddScoped(sp => new DevelopmentDataInitializer(
                sp.GetRequiredService<AdminWriteDbContext>(),
                sp.GetRequiredService<TimeProvider>()));
        }

        return builder;
    }

    /// <summary>
    /// Adds the Admin-owned integration-event worker composition: the Branding projection consumer that claims
    /// Catalog-database transport rows for consumer <c>admin</c> and writes only the Branding projection and inbox.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The application builder type.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddAdminIntegrationEventWorkerInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDbContextDevelopmentDiagnostics<AdminWriteDbContext>();
        }

        builder.AddAdminWriteDbContext();
        JsonTypeInfo<AdminTourCreatedIntegrationEvent> adminTourCreatedJsonTypeInfo =
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent;
        builder.Services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            adminTourCreatedJsonTypeInfo);
        builder.Services.AddDomainEventProcessing();
        builder.Services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        builder.Services.AddScoped<IBrandingProjectionStore, EfBrandingProjectionStore>();
        builder.Services.AddAdminBrandingProjection();

        builder.AddNpgsqlDbContext<AdminBrandingTransportDbContext>(
            ResourceNames.CatalogDatabase,
            configureDbContextOptions: options =>
            {
                ConfigureNpgsqlTracing(options);
                builder.Services.ApplyDbContextOptionConfigurations<AdminBrandingTransportDbContext>(options);
            });
        builder.Services.AddPostgreSqlIntegrationEventTransportConsumer<AdminBrandingTransportDbContext>(
            IntegrationEventConsumerNames.Admin);

        return builder;
    }

    private static TApplicationBuilder AddAdminRuntimeBackgroundServices<TApplicationBuilder>(
        this TApplicationBuilder builder,
        bool addRuntimeBackgroundServices)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        if (addRuntimeBackgroundServices)
        {
            builder.Services.AddHostedService<DocumentDraftRetentionHostedService>();
            builder.Services.AddHostedService<DocumentAuditRetentionHostedService>();
            builder.Services.AddIntegrationEventOutboxRelay<AdminWriteDbContext>();
            builder.Services.AddPostgreSqlIntegrationEventOutboxRelayAtomicClaims<AdminWriteDbContext>();
        }

        return builder;
    }

    private static void AddAdminWriteDbContext<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.Services.TryAddSingleton<IAuditTrailSink<DocumentAuditRecord>, DocumentAuditTrailSink>();
        builder.Services.AddDomainEventDispatch<AdminWriteDbContext>();
        builder.Services.AddIdempotencyStore<AdminWriteDbContext>();

        builder.AddNpgsqlDbContext<AdminWriteDbContext>(
            ResourceNames.AdminDatabase,
            configureDbContextOptions: options => ConfigureAdminWriteDbContext(builder, options));
    }

    private static void AddTourCapacityMutationLock<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddKeyedNpgsqlDataSource(
            ResourceNames.AdminDatabase,
            configureDataSourceBuilder: dataSourceBuilder =>
            {
                ConfigureNpgsqlDataSource(dataSourceBuilder);
                dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = Math.Min(
                    TourCapacityLockMaximumPoolSize,
                    dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize);
            });
        builder.Services.AddSingleton<ITourCapacityMutationLock>(serviceProvider =>
            new PostgreSqlTourCapacityMutationLock(
                serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(ResourceNames.AdminDatabase)));
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
        ConfigureNpgsqlTracing(options);
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        builder.Services.ApplyDbContextOptionConfigurations<AdminReadDbContext>(options);
    }

    private static void ConfigureAdminWriteDbContext<TApplicationBuilder>(
        TApplicationBuilder builder,
        DbContextOptionsBuilder options)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ConfigureNpgsqlTracing(options);
        builder.Services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
    }

    private static void ConfigureNpgsqlTracing(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(providerOptions => providerOptions.ConfigureDataSource(ConfigureNpgsqlDataSource));
    }

    private static void ConfigureNpgsqlDataSource(NpgsqlDataSourceBuilder dataSourceBuilder)
    {
        dataSourceBuilder.ConfigureTracingWithoutFirstResponseEvent();
    }

}
