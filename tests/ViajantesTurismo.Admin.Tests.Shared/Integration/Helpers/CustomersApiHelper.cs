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

    public static async Task<GetCustomerDto> CreateCustomerAndReadAsync(
        this HttpClient client,
        CreateCustomerDto request,
        CancellationToken cancellationToken)
    {
        var response = await CreateCustomerAsync(client, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetCustomerDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetCustomerAsync(
        this HttpClient client,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri($"/customers/{customerId}", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<CustomerDetailsDto> GetCustomerAndReadAsync(
        this HttpClient client,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var response = await GetCustomerAsync(client, customerId, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken))!;
    }

    public static async Task<HttpResponseMessage> GetAllCustomersAsync(
        this HttpClient client,
        CancellationToken cancellationToken)
    {
        return await client.GetAsync(
            new Uri("/customers", UriKind.Relative),
            cancellationToken);
    }

    public static async Task<GetCustomerDto[]> GetAllCustomersAndReadAsync(
        this HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await GetAllCustomersAsync(client, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetCustomerDto[]>(cancellationToken))!;
    }
}
