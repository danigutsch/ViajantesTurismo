using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Branding;
using SharedKernel.Domain;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Idempotency;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class AdminInfrastructureModuleTestScope(ServiceProvider serviceProvider) : IDisposable
{
    private readonly ServiceProvider serviceProvider = serviceProvider;
    private readonly IServiceScope scope = serviceProvider.CreateScope();

    public IDomainEventDispatcher Dispatcher => scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

    public AdminWriteDbContext WriteContext => scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();

    public IUnitOfWork UnitOfWork => scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    public IQueryService QueryService => scope.ServiceProvider.GetRequiredService<IQueryService>();

    public ITourStore TourStore => scope.ServiceProvider.GetRequiredService<ITourStore>();

    public ICustomerStore CustomerStore => scope.ServiceProvider.GetRequiredService<ICustomerStore>();

    public IDocumentStore DocumentStore => scope.ServiceProvider.GetRequiredService<IDocumentStore>();

    public IIntegrationEventOutbox Outbox => scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();

    public IBrandingApiClient BrandingApiClient => scope.ServiceProvider.GetRequiredService<IBrandingApiClient>();

    public IIdempotencyStore? IdempotencyStore => scope.ServiceProvider.GetService<IIdempotencyStore>();

    public IReadOnlyList<IDbContextConfiguration<AdminWriteDbContext>> DbContextConfigurations =>
        scope.ServiceProvider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();

    public IReadOnlyList<IHostedService> HostedServices => serviceProvider.GetServices<IHostedService>().ToArray();

    public bool CreateTourHandlerHasScopedLifetime()
    {
        using var firstScope = serviceProvider.CreateScope();
        using var secondScope = serviceProvider.CreateScope();
        var firstHandler = firstScope.ServiceProvider.GetRequiredService<CreateTourCommandHandler>();
        var sameScopeHandler = firstScope.ServiceProvider.GetRequiredService<CreateTourCommandHandler>();
        var secondHandler = secondScope.ServiceProvider.GetRequiredService<CreateTourCommandHandler>();

        return ReferenceEquals(firstHandler, sameScopeHandler) && !ReferenceEquals(firstHandler, secondHandler);
    }

    public void Dispose()
    {
        scope.Dispose();
        serviceProvider.Dispose();
    }
}
