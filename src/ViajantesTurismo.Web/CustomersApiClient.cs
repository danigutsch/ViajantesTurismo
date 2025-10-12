using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Web;

internal sealed class CustomersApiClient(HttpClient httpClient)
{
    public async Task<GetCustomerDto[]> GetCustomers(int maxItems = 100, CancellationToken cancellationToken = default)
    {
        List<GetCustomerDto>? customers = null;

        await foreach (var customer in httpClient.GetFromJsonAsAsyncEnumerable<GetCustomerDto>("/customers", cancellationToken))
        {
            if (customers?.Count >= maxItems)
            {
                break;
            }

            if (customer is null)
            {
                continue;
            }

            customers ??= [];
            customers.Add(customer);
        }

        return customers?.ToArray() ?? [];
    }

    public async Task<GetCustomerDto?> GetCustomerById(int id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<GetCustomerDto>($"/customers/{id}", cancellationToken);
    }

    public async Task<Uri> CreateCustomer(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        return response.Headers.Location ?? throw new InvalidOperationException("The Location header is missing in the response.");
    }
}