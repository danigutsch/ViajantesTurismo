using System.Net;
using System.Net.Mime;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Helpers;

namespace ViajantesTurismo.Admin.Web;

internal sealed class CustomersApiClient(HttpClient httpClient) : ICustomersApiClient
{
    public async Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken cancellationToken, int maxItems = 100)
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

    public async Task<CustomerDetailsDto?> GetCustomerById(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(new Uri($"/customers/{id}", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken);
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

    public async Task<ImportResultDto> ImportCustomers(byte[] fileContent, string fileName, CancellationToken cancellationToken)
    {
        using var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Text.Csv);
        using var content = new MultipartFormDataContent();
        content.Add(fileBytes, "file", fileName);

        var response = await httpClient.PostAsync(new Uri("/customers/import", UriKind.Relative), content, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ImportResultDto>(cancellationToken)
               ?? throw new InvalidOperationException("The import response body was empty.");
    }

    public async Task<ImportResultDto> CommitImportWithResolutions(byte[] fileContent, string fileName, IReadOnlyDictionary<string, string> conflictResolutions, CancellationToken cancellationToken)
    {
        using var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Text.Csv);
        using var content = new MultipartFormDataContent();
        content.Add(fileBytes, "file", fileName);
        content.Add(new StringContent(ConflictResolutionSerialization.Serialize(conflictResolutions)), "conflictResolutions");

        var response = await httpClient.PostAsync(new Uri("/customers/import/commit", UriKind.Relative), content, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ImportResultDto>(cancellationToken)
               ?? throw new InvalidOperationException("The import response body was empty.");
    }
}
