using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>Registers the customer import workflow and its application services.</summary>
internal static class CustomerImportServiceCollectionExtensions
{
    /// <summary>Adds the scoped customer import workflow and conflict detector.</summary>
    /// <param name="services">The Admin application service collection.</param>
    public static void AddCustomerImport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<CustomerImportConflictDetector>();
        services.TryAddScoped<CustomerImportWorkflowService>();
    }
}
