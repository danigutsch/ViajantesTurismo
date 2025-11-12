using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Helpers;

namespace ViajantesTurismo.Admin.Web;

internal sealed class CustomersApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken cancellationToken,
        int maxItems = 100)
    {
        List<GetCustomerDto>? customers = null;

        await foreach (var customer in httpClient.GetFromJsonAsAsyncEnumerable<GetCustomerDto>("/customers",
                           cancellationToken))
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

    public async Task<CustomerDetailsDto?> GetCustomerById(Guid id, CancellationToken cancellationToken)
    {
        return await httpClient.GetFromJsonAsync<CustomerDetailsDto>($"/customers/{id}", cancellationToken);
    }

    public async Task<Uri> CreateCustomer(CreateCustomerDto dto, CancellationToken cancellationToken)
    {
        var response =
            await httpClient.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), dto, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);

        return response.Headers.Location ??
               throw new InvalidOperationException("The Location header is missing in the response.");
    }

    public async Task UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync($"/customers/{id}", dto, cancellationToken);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }
}
