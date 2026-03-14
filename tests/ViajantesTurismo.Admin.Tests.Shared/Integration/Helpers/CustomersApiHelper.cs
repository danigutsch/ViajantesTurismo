using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

/// <summary>
/// Helper methods for Customers API operations in integration tests.
/// </summary>
public static class CustomersApiHelper
{
    public static async Task<HttpResponseMessage> CreateCustomerAsync(
        this HttpClient client,
        CreateCustomerDto request,
        CancellationToken cancellationToken)
    {
        return await client.PostAsJsonAsync(
            new Uri("/customers", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<HttpResponseMessage> GetAllCustomersAsync(
        this HttpClient client,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri("/customers", UriKind.Relative),
            cancellationToken);
    }
}
