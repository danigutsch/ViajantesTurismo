using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.AuditTrail;
using SharedKernel.Domain;
using SharedKernel.Domain.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.CloudEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class AdminWriteDbContextTestFactory
{
    public static AdminWriteDbContextTestScope CreateWithDomainEventDispatcher(
        IDomainEventDispatcher dispatcher,
        params IInterceptor[] additionalInterceptors)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        var services = new ServiceCollection();
        services.AddSingleton(dispatcher);
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddDbContextPool<AdminWriteDbContext>(
            (provider, options) =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
                services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
                options.AddInterceptors(additionalInterceptors);
            },
            poolSize: 1);

        var provider = services.BuildServiceProvider();
        try
        {
            return new AdminWriteDbContextTestScope(provider);
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }

    public static AdminWriteDbContextTestScope CreateWithGeneratedIntegrationEventDispatcher(
        DomainEventDispatchLifecycleProbe? probe = null,
        params IInterceptor[] additionalInterceptors)
    {
        var services = new ServiceCollection();
        services.AddAdminIntegrationEventContract();
        if (probe is not null)
        {
            services.AddSingleton(probe);
            services.AddScoped<IDomainEventDispatchHandler, AsyncOnlyDomainEventDispatchProbeHandler>();
        }

        services.AddDomainEventProcessing();
        services.AddSingleton<IAuditTrailSink<DocumentAuditRecord>, CapturingDocumentAuditTrailSink>();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddDbContextPool<AdminWriteDbContext>(
            (provider, options) =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
                services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
                options.AddInterceptors(additionalInterceptors);
            },
            poolSize: 1);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        try
        {
            return new AdminWriteDbContextTestScope(provider);
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }

    public static ServiceProvider CreateOutboxRelayProvider(
        IEventEnvelopePublisher publisher,
        TimeProvider timeProvider,
        Action<IntegrationEventOutboxRelayOptions>? configureOptions = null,
        string? databaseName = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(publisher);
        services.AddSingleton(timeProvider);
        services.AddAdminIntegrationEventContract();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddIntegrationEventOutboxRelay<AdminWriteDbContext>(configureOptions);
        var resolvedDatabaseName = databaseName ?? Guid.NewGuid().ToString("N");
        services.AddDbContext<AdminWriteDbContext>(options =>
        {
            options.UseInMemoryDatabase(resolvedDatabaseName);
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
        });
        configureServices?.Invoke(services);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    public static async ValueTask EnqueueAdminTourCreatedIntegrationEvent(ServiceProvider provider, string slug)
    {
        ArgumentNullException.ThrowIfNull(provider);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        await outbox.Enqueue(new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            new DateTimeOffset(2026, 6, 22, 11, 59, 0, TimeSpan.Zero),
            Guid.CreateVersion7(),
            slug,
            "Andes Relay 2026"), TestContext.Current.CancellationToken);
        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public static IntegrationEventOutboxMessage GetSingleOutboxMessage(ServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>()
            .Set<IntegrationEventOutboxMessage>()
            .ShouldHaveSingleItem();
    }

    public static void ClaimSingleOutboxMessage(ServiceProvider provider, DateTimeOffset claimedUntil)
    {
        ArgumentNullException.ThrowIfNull(provider);

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var message = dbContext.Set<IntegrationEventOutboxMessage>().ShouldHaveSingleItem();
        message.MarkClaimed("test-relay", claimedUntil);
        _ = dbContext.SaveChanges();
    }

    public static ServiceDescriptor GetPostgreSqlOutboxRelayClaimStrategyDescriptor()
    {
        var services = new ServiceCollection();
        services.AddIntegrationEventOutboxRelay<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventOutboxRelayAtomicClaims<AdminWriteDbContext>();

        return services
            .Where(service => service.ServiceType == typeof(IIntegrationEventOutboxClaimStrategy<AdminWriteDbContext>))
            .ShouldHaveSingleItem();
    }

    public static ServiceDescriptor GetPostgreSqlTransportConsumerHostedServiceDescriptor()
    {
        var services = new ServiceCollection();
        services.AddPostgreSqlIntegrationEventTransportConsumer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);

        return services
            .Where(service => service.ServiceType == typeof(IHostedService))
            .ShouldHaveSingleItem();
    }

    public static IntegrationEventTransportScenario CreateTransportScenario(TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(timeProvider ?? TimeProvider.System);
        services.AddPostgreSqlIntegrationEventTransportProducer<CatalogIntegrationTransportDbContext>(IntegrationEventConsumerNames.Catalog);
        services.AddDbContext<CatalogIntegrationTransportDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
            services.ApplyDbContextOptionConfigurations<CatalogIntegrationTransportDbContext>(options);
        });

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        try
        {
            return new IntegrationEventTransportScenario(provider);
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }

    public static EventEnvelope CreateEnvelope(string eventId) => new(
        CloudEventConstants.Spec,
        CloudEventConstants.SpecVersion,
        eventId,
        new Uri("urn:viajantes:admin"),
        AdminTourCreatedIntegrationEvent.EventType,
        AdminTourCreatedIntegrationEvent.EventVersion,
        new DateTimeOffset(2026, 6, 22, 11, 59, 0, TimeSpan.Zero),
        "tour-1",
        "application/json",
        null,
        "{}",
        EventPayloadEncoding.Json,
        null);

}
