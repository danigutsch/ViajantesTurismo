using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.AuditTrail;
using PandaBear.Domain;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;
using ViajantesTurismo.Admin.UnitTests.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Application.DomainEvents;

/// <summary>
/// Disposable DI scope exposing a real domain event dispatcher wired to a capturing outbox.
/// </summary>
/// <param name="serviceProvider">The provider disposed with this scope.</param>
/// <param name="dispatcher">The dispatcher resolved from the provider.</param>
/// <param name="outbox">The outbox that captures integration events.</param>
internal sealed class TestDomainEventDispatchScope(
    ServiceProvider serviceProvider,
    IDomainEventDispatcher dispatcher,
    CapturingIntegrationEventOutbox outbox) : IDisposable
{
    /// <summary>The domain event dispatcher under test.</summary>
    public IDomainEventDispatcher Dispatcher => dispatcher;

    /// <summary>The outbox capturing integration events enqueued by the dispatcher.</summary>
    public CapturingIntegrationEventOutbox Outbox => outbox;

    /// <summary>Builds the application services with fake persistence and a fixed clock at the given time.</summary>
    public static TestDomainEventDispatchScope Create(DateTimeOffset now)
    {
        var builder = Host.CreateApplicationBuilder();
        var unitOfWork = new FakeUnitOfWork();
        var outbox = new CapturingIntegrationEventOutbox(unitOfWork);

        builder.AddApplication();
        builder.Services.AddSingleton<IIntegrationEventOutbox>(_ => outbox);
        builder.Services.AddSingleton<IDomainEventIntegrationEventOutbox>(_ => outbox);
        builder.Services.AddSingleton<IAuditTrailSink<DocumentAuditRecord>, CapturingDocumentAuditTrailSink>();
        builder.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));

        var serviceProvider = builder.Services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();

        return new TestDomainEventDispatchScope(serviceProvider, dispatcher, outbox);
    }

    /// <summary>Disposes the underlying service provider.</summary>
    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
