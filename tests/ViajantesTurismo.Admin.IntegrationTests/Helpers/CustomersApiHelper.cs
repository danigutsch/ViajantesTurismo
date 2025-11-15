using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for Customers API operations in integration tests.
/// </summary>
internal static class CustomersApiHelper
{
    public static async Task<HttpResponseMessage> CreateCustomerAsync(
        HttpClient client,
        CreateCustomerDto request,
        CancellationToken cancellationToken = default)
    {
        return await client.PostAsJsonAsync(
            new Uri("/customers", UriKind.Relative),
            request,
            cancellationToken);
    }

    public static async Task<GetCustomerDto> CreateCustomerAndReadAsync(
        HttpClient client,
        CreateCustomerDto request,
        CancellationToken cancellationToken = default)
    {
        var response = await CreateCustomerAsync(client, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetCustomerDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetCustomerAsync(
        HttpClient client,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await client.GetAsync(
            new Uri($"/customers/{customerId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<CustomerDetailsDto> GetCustomerAndReadAsync(
        HttpClient client,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var response = await GetCustomerAsync(client, customerId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetAllCustomersAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        return await client.GetAsync(
            new Uri("/customers", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetCustomerDto[]> GetAllCustomersAndReadAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        var response = await GetAllCustomersAsync(client, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetCustomerDto[]>(cancellationToken))!;
    }
}
