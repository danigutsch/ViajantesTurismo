using SharedKernel.AspNetCore.Authentication.Bff;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;

namespace ViajantesTurismo.Management.Web.Components.Pages.Customers;

/// <summary>Composes the Management Web customer workflows.</summary>
internal static class CustomerWorkflowServiceCollectionExtensions
{
    /// <summary>Registers the wizard state and the customer API client.</summary>
    internal static void AddCustomerWorkflows(this IServiceCollection services)
    {
        services.AddScoped<CustomerCreationState>();
        services.AddHttpClient<ICustomersApiClient, CustomersApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
            .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
    }
}
