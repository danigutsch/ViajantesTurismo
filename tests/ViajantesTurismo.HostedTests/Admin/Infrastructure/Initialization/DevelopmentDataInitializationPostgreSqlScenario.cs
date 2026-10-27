using PostgreSqlTestCleanup = ViajantesTurismo.HostedTests.Infrastructure.HostedTestCleanup;
using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.AuditTrail;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Initialization;

/// <summary>Runs development data initialization against a real PostgreSQL database.</summary>
internal sealed class DevelopmentDataInitializationPostgreSqlScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase database;
    private readonly string connectionString;

    private DevelopmentDataInitializationPostgreSqlScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
        connectionString = database.ConnectionString;
    }

    /// <summary>Creates a migrated scenario over a new fixture-issued database, disposing it if migration fails.</summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Failed migration setup must independently clean its database and preserve both failures.")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The returned scenario owns and disposes the fixture-issued database.")]
    public static async ValueTask<DevelopmentDataInitializationPostgreSqlScenario> Create(
        PostgreSqlTestServerFixture fixture,
        CancellationToken ct)
    {
        var database = await fixture.CreateDatabase(ct);
        var scenario = new DevelopmentDataInitializationPostgreSqlScenario(database);
        try
        {
            await scenario.MigrateToLatest(ct);
        }
        catch (Exception creationFailure)
        {
            await PostgreSqlTestCleanup.DisposeResources(creationFailure, scenario);
            throw;
        }

        return scenario;
    }

    /// <summary>Runs the initializer with a save interceptor that fails after saving, before the commit.</summary>
    public async Task InitializeWithFailure(CancellationToken ct)
    {
        await using var provider = CreateProvider(new FailAfterSaveInterceptor());
        await using var scope = provider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();
        await initializer.Initialize(ct);
    }

    /// <summary>Runs the initializer with a save interceptor that cancels after saving, before the commit.</summary>
    public async Task InitializeWithCancellation(CancellationToken ct)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        await using var provider = CreateProvider(new CancelAfterSaveInterceptor(cancellation));
        await using var scope = provider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();
        await initializer.Initialize(cancellation.Token);
    }

    /// <summary>Runs the development data initializer once.</summary>
    public async Task Initialize(CancellationToken ct)
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DevelopmentDataInitializer>();
        await initializer.Initialize(ct);
    }

    /// <summary>Runs two initializers at once, holding both at the save barrier before letting them proceed.</summary>
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

    /// <summary>Counts the stored tours, customers, bookings and outbox messages.</summary>
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

    /// <summary>Disposes the fixture-issued database.</summary>
    public async ValueTask DisposeAsync() => await database.DisposeAsync();

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
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options, enableDevelopmentDiagnostics: false);
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
