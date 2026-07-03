using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ViajantesTurismo.Common.Contracts;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// HTTP client for the Admin customer API.
/// </summary>
public sealed partial class CustomersApiClient(HttpClient httpClient, ILogger<CustomersApiClient> logger) : ICustomersApiClient
{
    private static readonly CustomersApiClientJsonContext Json = CustomersApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken ct, int maxItems = 100)
    {
        if (maxItems <= 0)
        {
            return [];
        }

        List<GetCustomerDto>? customers = null;

        await foreach (var customer in httpClient.GetFromJsonAsAsyncEnumerable("/customers", Json.GetCustomerDto, ct).ConfigureAwait(false))
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

    /// <inheritdoc />
    public async Task<CustomerDetailsDto?> GetCustomerById(Guid id, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(new Uri($"/customers/{id}", UriKind.Relative), ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(Json.CustomerDetailsDto, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CustomerCreateOutcomeDto> CreateCustomer(CreateCustomerDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var activity = AdminContractsClientTelemetry.ActivitySource.StartActivity(
            AdminContractsClientTelemetry.CreateCustomerActivity,
            ActivityKind.Client);
        using var response = await httpClient.PostAsJsonAsync(
            new Uri("/customers", UriKind.Relative),
            dto,
            Json.CreateCustomerDto,
            ct).ConfigureAwait(false);

        CustomerCreateOutcomeDto outcome;
        if (response.StatusCode is HttpStatusCode.Created)
        {
            outcome = new CustomerCreateOutcomeDto
            {
                Kind = CustomerCreateOutcomeKind.Succeeded,
                StatusCode = response.StatusCode,
                Location = response.Headers.Location
            };
        }
        else if (response.StatusCode is HttpStatusCode.BadRequest)
        {
            outcome = await ReadValidationProblem(response, ct).ConfigureAwait(false);
        }
        else
        {
            outcome = CreateStatusOutcome(MapStatusCode(response.StatusCode), response.StatusCode);
        }

        activity?.SetTag(AdminContractsClientTelemetry.StatusCodeTag, (int)outcome.StatusCode);
        activity?.SetTag(AdminContractsClientTelemetry.OutcomeKindTag, outcome.Kind.ToString());
        if (outcome.Kind != CustomerCreateOutcomeKind.Succeeded)
        {
            LogCustomerCreateOutcome(logger, outcome.StatusCode, outcome.Kind);
        }

        return outcome;
    }

    /// <inheritdoc />
    public async Task UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PutAsJsonAsync(
            $"/customers/{id}",
            dto,
            Json.UpdateCustomerDto,
            ct).ConfigureAwait(false);
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(
            response,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ImportResultDto> ImportCustomers(byte[] fileContent, string fileName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileContent);
        ArgumentNullException.ThrowIfNull(fileName);

        using var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Text.Csv);
        using var content = new MultipartFormDataContent();
        content.Add(fileBytes, "file", fileName);

        using var response = await httpClient.PostAsync(new Uri("/customers/import", UriKind.Relative), content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(Json.ImportResultDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The import response body was empty.");
    }

    /// <inheritdoc />
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

        using var response = await httpClient.PostAsync(new Uri("/customers/import/commit", UriKind.Relative), content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(Json.ImportResultDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The import response body was empty.");
    }

    private static async Task<CustomerCreateOutcomeDto> ReadValidationProblem(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            return CreateStatusOutcome(CustomerCreateOutcomeKind.EmptyBody, response.StatusCode, "Validation problem response body was empty.");
        }

        try
        {
            var problem = JsonSerializer.Deserialize(content, Json.ContractValidationProblemDto);
            if (problem?.Errors is not { Count: > 0 })
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

    private static CustomerCreateOutcomeKind MapStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => CustomerCreateOutcomeKind.NotFound,
            HttpStatusCode.Unauthorized => CustomerCreateOutcomeKind.Unauthorized,
            HttpStatusCode.Forbidden => CustomerCreateOutcomeKind.Forbidden,
            HttpStatusCode.Conflict => CustomerCreateOutcomeKind.Conflict,
            _ => CustomerCreateOutcomeKind.UnexpectedStatus
        };
    }

    [LoggerMessage(1, LogLevel.Warning, "Customer create returned {StatusCode} with outcome {OutcomeKind}.")]
    private static partial void LogCustomerCreateOutcome(ILogger logger, HttpStatusCode statusCode, CustomerCreateOutcomeKind outcomeKind);

}
