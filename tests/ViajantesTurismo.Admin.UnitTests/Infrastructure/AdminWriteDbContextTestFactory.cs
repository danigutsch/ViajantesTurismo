using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvents;
using SharedKernel.DomainEvents.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Admin.Infrastructure;

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
        services.AddIntegrationEventOutbox<AdminWriteDbContext>(ServiceLifetime.Singleton);
        services.AddDbContext<AdminWriteDbContext>((provider, options) =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
            options.AddInterceptors(additionalInterceptors);
        });

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

    public static AdminWriteDbContextTestScope CreateWithGeneratedIntegrationEventDispatcher()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventSerializer, AdminIntegrationEventSerializer>();
        services.AddDomainEventProcessing();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>(ServiceLifetime.Singleton);
        services.AddDbContext<AdminWriteDbContext>((provider, options) =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
        });

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

    public static ServiceProvider CreateOutboxRelayProvider(IEventEnvelopePublisher publisher, TimeProvider timeProvider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(publisher);
        services.AddSingleton(timeProvider);
        services.AddSingleton<IIntegrationEventSerializer, AdminIntegrationEventSerializer>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddSingleton<EfIntegrationEventOutboxRelay<AdminWriteDbContext>>();
        var databaseName = Guid.NewGuid().ToString("N");
        services.AddDbContext<AdminWriteDbContext>(options =>
        {
            options.UseInMemoryDatabase(databaseName);
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
        });

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

}
