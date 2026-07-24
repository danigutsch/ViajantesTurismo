using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.AuditTrail;
using SharedKernel.Domain.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Idempotency.EntityFrameworkCore;
using SharedKernel.IntegrationTesting;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Initialization;

internal sealed class DevelopmentDataInitializationPostgreSqlScenario : IAsyncDisposable
{
    private const string PostgreSqlResourceName = "postgres";
    private const string DatabaseResourceName = "admin-initialization";

    private readonly AspireTestApplication app;
    private readonly string connectionString;

    private DevelopmentDataInitializationPostgreSqlScenario(AspireTestApplication app, string connectionString)
    {
        this.app = app;
        this.connectionString = connectionString;
    }

    public static async ValueTask<DevelopmentDataInitializationPostgreSqlScenario> Create(CancellationToken ct)
    {
        var appBuilder = AspireTestApplication.CreateBuilder();
        var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
        _ = databaseServer.AddDatabase(DatabaseResourceName);

        var app = await AspireTestApplication.Start(appBuilder, [PostgreSqlResourceName], null, ct);
        var connectionString = await app.GetConnectionString(DatabaseResourceName, ct);
        var scenario = new DevelopmentDataInitializationPostgreSqlScenario(app, connectionString);
        await scenario.MigrateToLatest(ct);

        return scenario;
    }

    public async Task InitializeWithFailure(CancellationToken ct)
    {
        await using var provider = CreateProvider(new FailAfterSaveInterceptor());
        await using var scope = provider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();
        await initializer.Initialize(ct);
    }

    public async Task InitializeWithCancellation(CancellationToken ct)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        await using var provider = CreateProvider(new CancelAfterSaveInterceptor(cancellation));
        await using var scope = provider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();
        await initializer.Initialize(cancellation.Token);
    }

    public async Task Initialize(CancellationToken ct)
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();
        await initializer.Initialize(ct);
    }

    public async Task InitializeConcurrently(CancellationToken ct)
    {
        var barrier = new ConcurrentInitializationSaveBarrierInterceptor();
        await using var firstProvider = CreateProvider(barrier);
        await using var secondProvider = CreateProvider(barrier);
        await using var firstScope = firstProvider.CreateAsyncScope();
        await using var secondScope = secondProvider.CreateAsyncScope();
        var firstInitializer = firstScope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();
        var secondInitializer = secondScope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();

        var firstInitialization = firstInitializer.Initialize(ct);
        var secondInitialization = secondInitializer.Initialize(ct);
        await barrier.BothSaving.WaitAsync(ct);
        barrier.Release();
        await Task.WhenAll(firstInitialization, secondInitialization);
    }

    public async ValueTask<(int Tours, int Customers, int Bookings, int Outbox)> CountData(CancellationToken ct)
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var tours = await dbContext.Tours.CountAsync(ct);
        var customers = await dbContext.Customers.CountAsync(ct);
        var bookings = await dbContext.Set<Booking>().CountAsync(ct);
        var outbox = await dbContext.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM messaging.outbox_messages")
            .SingleAsync(ct);

        return (tours, customers, bookings, outbox);
    }

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    private async Task MigrateToLatest(CancellationToken ct)
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        await dbContext.Database.MigrateAsync(ct);
    }

    private ServiceProvider CreateProvider(IInterceptor? interceptor = null)
    {
        var services = new ServiceCollection();
        JsonTypeInfo<AdminTourCreatedIntegrationEvent> adminTourCreatedJsonTypeInfo =
            AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent;
        services.AddIntegrationEventContract(
            AdminTourCreatedIntegrationEvent.EventType,
            adminTourCreatedJsonTypeInfo);
        services.AddSingleton<IAuditTrailSink<DocumentAuditRecord>, DocumentAuditTrailSink>();
        services.AddDomainEventProcessing();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        services.AddDbContext<AdminWriteDbContext>((_, options) =>
        {
            options.UseNpgsql(connectionString);
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
            if (interceptor is not null)
            {
                options.AddInterceptors(interceptor);
            }
        });
        services.AddScoped(serviceProvider => new DevelopmentDataInitializer(
            serviceProvider.GetRequiredService<AdminWriteDbContext>(),
            serviceProvider.GetRequiredService<TimeProvider>()));

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
