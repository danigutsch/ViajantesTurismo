using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Api;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure.Customers;

/// <summary>Hosts the customer write routes with their real handlers and an isolated store.</summary>
internal sealed class CustomerMutationTestHost : IAsyncDisposable
{
    private readonly WebApplicationFactory<AdminApiHostEntryPoint> factory;

    /// <summary>Creates an isolated API with an optional customer projection.</summary>
    /// <param name="customers">The customer store shared with the test.</param>
    /// <param name="projectCustomer">The projection to return for customer IDs present in the store.</param>
    public CustomerMutationTestHost(FakeCustomerStore customers, Func<Guid, CustomerDetailsDto>? projectCustomer = null)
    {
        factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<ICustomerStore>(_ => customers));
            services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, FakeUnitOfWork>());
            services.Replace(ServiceDescriptor.Scoped<IQueryService>(_ => new CustomerMutationQueryService(customers, projectCustomer)));
        });
        Client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(Client, "Admin");
    }

    /// <summary>Gets the authenticated client for the isolated API.</summary>
    public HttpClient Client
    {
        get;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await factory.DisposeAsync();
    }
}
