using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.AuditTrail;
using SharedKernel.DomainEvents;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Application.IntegrationEvents;
using ViajantesTurismo.Admin.UnitTests.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Application.DomainEvents;

internal sealed class TestDomainEventDispatchScope(
    ServiceProvider serviceProvider,
    IDomainEventDispatcher dispatcher,
    CapturingIntegrationEventOutbox outbox) : IDisposable
{
    public IDomainEventDispatcher Dispatcher => dispatcher;

    public CapturingIntegrationEventOutbox Outbox => outbox;

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

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
