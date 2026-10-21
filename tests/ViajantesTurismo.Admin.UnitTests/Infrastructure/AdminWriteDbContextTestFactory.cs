using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.AuditTrail;
using PandaBear.Domain;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Documents;
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
        services.AddAdminIntegrationEventContract();
        if (dispatcher is OutboxEnqueuingDomainEventDispatcher enqueuingDispatcher)
        {
            services.AddSingleton(enqueuingDispatcher.TimeProvider);
        }
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

}
