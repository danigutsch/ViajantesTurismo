using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.Web;

internal sealed class CustomersApiClient(HttpClient httpClient) : ICustomersApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken ct, int maxItems = 100)
    {
        List<GetCustomerDto>? customers = null;

        await foreach (var customer in httpClient.GetFromJsonAsAsyncEnumerable<GetCustomerDto>("/customers", ct))
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

    public async Task<CustomerDetailsDto?> GetCustomerById(Guid id, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(new Uri($"/customers/{id}", UriKind.Relative), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(ct);
    }

    public async Task<CustomerCreateOutcomeDto> CreateCustomer(CreateCustomerDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), dto, ct);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            return new CustomerCreateOutcomeDto
            {
                Kind = CustomerCreateOutcomeKind.Succeeded,
                StatusCode = response.StatusCode,
                Location = response.Headers.Location
            };
        }

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => await ReadValidationProblem(response, ct),
            HttpStatusCode.NotFound => CreateStatusOutcome(CustomerCreateOutcomeKind.NotFound, response.StatusCode),
            HttpStatusCode.Unauthorized => CreateStatusOutcome(CustomerCreateOutcomeKind.Unauthorized, response.StatusCode),
            HttpStatusCode.Forbidden => CreateStatusOutcome(CustomerCreateOutcomeKind.Forbidden, response.StatusCode),
            HttpStatusCode.Conflict => CreateStatusOutcome(CustomerCreateOutcomeKind.Conflict, response.StatusCode),
            _ => CreateStatusOutcome(CustomerCreateOutcomeKind.UnexpectedStatus, response.StatusCode)
        };
    }

    public async Task UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var response = await httpClient.PutAsJsonAsync($"/customers/{id}", dto, ct);
        await ValidationErrorHelper.EnsureSuccessOrThrowValidationException(response);
    }

    public async Task<ImportResultDto> ImportCustomers(byte[] fileContent, string fileName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileContent);
        ArgumentNullException.ThrowIfNull(fileName);

        using var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Text.Csv);
        using var content = new MultipartFormDataContent();
        content.Add(fileBytes, "file", fileName);

        var response = await httpClient.PostAsync(new Uri("/customers/import", UriKind.Relative), content, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ImportResultDto>(ct)
               ?? throw new InvalidOperationException("The import response body was empty.");
    }

    public async Task<ImportResultDto> CommitImportWithResolutions(byte[] fileContent, string fileName, IReadOnlyDictionary<string, string> conflictResolutions, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileContent);
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(conflictResolutions);

        using var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Text.Csv);
        using var content = new MultipartFormDataContent();
        content.Add(fileBytes, "file", fileName);
        content.Add(new StringContent(ConflictResolutionSerialization.Serialize(conflictResolutions)), "conflictResolutions");

        var response = await httpClient.PostAsync(new Uri("/customers/import/commit", UriKind.Relative), content, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ImportResultDto>(ct)
               ?? throw new InvalidOperationException("The import response body was empty.");
    }

    private static async Task<CustomerCreateOutcomeDto> ReadValidationProblem(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(content))
        {
            return CreateStatusOutcome(CustomerCreateOutcomeKind.EmptyBody, response.StatusCode, "Validation problem response body was empty.");
        }

        try
        {
            var problem = JsonSerializer.Deserialize<ValidationProblemDetails>(content, JsonOptions);
            if (problem?.Errors is null || problem.Errors.Count == 0)
            {
                return CreateStatusOutcome(CustomerCreateOutcomeKind.MalformedBody, response.StatusCode, "Validation problem response body did not contain errors.");
            }

            return new CustomerCreateOutcomeDto
            {
                Kind = CustomerCreateOutcomeKind.ValidationProblem,
                StatusCode = response.StatusCode,
                ValidationErrors = new Dictionary<string, string[]>(problem.Errors, StringComparer.Ordinal)
            };
        }
        catch (JsonException exception)
        {
            return CreateStatusOutcome(CustomerCreateOutcomeKind.MalformedBody, response.StatusCode, exception.Message);
        }
    }

    private static CustomerCreateOutcomeDto CreateStatusOutcome(CustomerCreateOutcomeKind kind, HttpStatusCode statusCode, string? message = null)
    {
        return new CustomerCreateOutcomeDto
        {
            Kind = kind,
            StatusCode = statusCode,
            Message = message
        };
    }
}
