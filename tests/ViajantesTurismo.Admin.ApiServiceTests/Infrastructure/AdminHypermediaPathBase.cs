using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Api;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Hosts the Admin endpoints below an explicit URL path prefix.</summary>
internal sealed class AdminHypermediaPathBase : IStartupFilter
{
    /// <summary>The prefix used to mount the test API.</summary>
    internal const string Prefix = "/admin-proxy";

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.UsePathBase(Prefix);
        next(app);
    };

    /// <summary>Configures the real endpoints and mediator with deterministic test stores.</summary>
    internal static (WebApplicationFactory<AdminApiHostEntryPoint> Factory, AdminHypermediaQueryProbe Queries) CreateHost()
    {
        var customers = new FakeCustomerStore();
        var tours = new FakeTourStore();
        var queries = new AdminHypermediaQueryProbe(customers, tours);
        var factory = AdminApiTestHost.Create(services =>
        {
            services.AddSingleton<IStartupFilter, AdminHypermediaPathBase>();
            services.Replace(ServiceDescriptor.Singleton<ICustomerStore>(customers));
            services.Replace(ServiceDescriptor.Singleton<ITourStore>(tours));
            services.Replace(ServiceDescriptor.Singleton<ITourCommandStore>(tours));
            services.Replace(ServiceDescriptor.Singleton<IQueryService>(queries));
            services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, FakeUnitOfWork>());
        });
        return (factory, queries);
    }
}
