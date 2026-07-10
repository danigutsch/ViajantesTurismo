using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

/// <summary>
/// Helper methods for Customers API operations in integration tests.
/// </summary>
public static class CustomersApiHelper
{
    private const string RoutePrefix = "/api/v1/customers";

    public static async Task<HttpResponseMessage> CreateCustomer(
        this HttpClient client,
        CreateCustomerDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        return await client.PostAsJsonAsync(
            new Uri(RoutePrefix, UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> GetAllCustomers(
        this HttpClient client,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        return await client.GetAsync(
            new Uri(RoutePrefix, UriKind.Relative),
            cancellationToken);
    }
}
