using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using Microsoft.Extensions.Logging;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// HTTP client for the Admin customer API.
/// </summary>
public sealed partial class CustomersApiClient(HttpClient httpClient, ILogger<CustomersApiClient> logger) : ICustomersApiClient
{
    private const string RoutePrefix = "/api/v1/customers";
    private static readonly CustomersApiClientJsonContext Json = CustomersApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken ct, int maxItems = 100)
    {
        if (maxItems <= 0)
        {
            return [];
        }

        List<GetCustomerDto>? customers = null;

        await foreach (var customer in httpClient.GetFromJsonAsAsyncEnumerable(RoutePrefix, Json.GetCustomerDto, ct).ConfigureAwait(false))
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
        using var response = await httpClient.GetAsync(new Uri($"{RoutePrefix}/{id}", UriKind.Relative), ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(Json.CustomerDetailsDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The customer response body was empty.");
    }

    /// <inheritdoc />
    public async Task<ContractCommandOutcomeDto> CreateCustomer(CreateCustomerDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var activity = AdminContractsClientTelemetry.ActivitySource.StartActivity(
            AdminContractsClientTelemetry.CreateCustomerActivity,
            ActivityKind.Client);
        activity?.SetTag(AdminContractsClientTelemetry.ApiAreaTag, AdminContractsClientTelemetry.AdminApiArea);
        activity?.SetTag(AdminContractsClientTelemetry.OperationTag, AdminContractsClientTelemetry.CreateCustomerActivity);

        using var response = await httpClient.PostAsJsonAsync(
            new Uri(RoutePrefix, UriKind.Relative),
            dto,
            Json.CreateCustomerDto,
            ct).ConfigureAwait(false);

        var outcome = await ContractCommandOutcome.FromResponse(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);

        activity?.SetTag(AdminContractsClientTelemetry.StatusCodeTag, (int)outcome.StatusCode);
        activity?.SetTag(AdminContractsClientTelemetry.CommandOutcomeKindTag, outcome.Kind.ToString());
        if (outcome.Kind != ContractCommandOutcomeKind.Succeeded)
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
            $"{RoutePrefix}/{id}",
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

        using var response = await httpClient.PostAsync(new Uri($"{RoutePrefix}/import", UriKind.Relative), content, ct).ConfigureAwait(false);
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

        using var response = await httpClient.PostAsync(new Uri($"{RoutePrefix}/import/commit", UriKind.Relative), content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(Json.ImportResultDto, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException("The import response body was empty.");
    }

    [LoggerMessage(1, LogLevel.Warning, "Customer create returned {StatusCode} with outcome {OutcomeKind}.")]
    private static partial void LogCustomerCreateOutcome(ILogger logger, HttpStatusCode statusCode, ContractCommandOutcomeKind outcomeKind);

}
