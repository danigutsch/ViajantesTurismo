using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using Microsoft.Extensions.Logging;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>
/// HTTP client for the Admin customer API.
/// </summary>
public sealed partial class CustomersApiClient(HttpClient httpClient, ILogger<CustomersApiClient> logger) : ICustomersApiClient
{
    private const string RoutePrefix = "/api/v1/customers";
    private static readonly CustomersApiClientJsonContext Json = CustomersApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<HttpOutcome<GetCustomerDto[]>> GetCustomers(CancellationToken ct, int maxItems = 100)
    {
        if (maxItems <= 0)
        {
            return HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, Array.Empty<GetCustomerDto>());
        }

        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri(RoutePrefix, UriKind.Relative)),
            HttpStatusCode.OK,
            Json.GetCustomerDtoArray,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);

        return outcome.Kind == HttpOutcomeKind.Succeeded
            ? outcome with { Value = outcome.Value?.OfType<GetCustomerDto>().Take(maxItems).ToArray() ?? [] }
            : outcome;
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<CustomerDetailsDto>> GetCustomerById(Guid id, CancellationToken ct)
    {
        return await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/{id}", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.CustomerDetailsDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> CreateCustomer(CreateCustomerDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var activity = AdminContractsClientTelemetry.ActivitySource.StartActivity(
            AdminContractsClientTelemetry.CreateCustomerActivity,
            ActivityKind.Client);
        activity?.SetTag(AdminContractsClientTelemetry.ApiAreaTag, AdminContractsClientTelemetry.AdminApiArea);
        activity?.SetTag(AdminContractsClientTelemetry.OperationTag, AdminContractsClientTelemetry.CreateCustomerActivity);

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(RoutePrefix, UriKind.Relative))
        {
            Content = JsonContent.Create(dto, Json.CreateCustomerDto)
        };
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            request,
            HttpStatusCode.Created,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);

        RecordOutcome(activity, outcome);
        return outcome;
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var request = new HttpRequestMessage(HttpMethod.Put, new Uri($"{RoutePrefix}/{id}", UriKind.Relative))
        {
            Content = JsonContent.Create(dto, Json.UpdateCustomerDto)
        };
        return await HttpOutcomeMapper.Send(
            httpClient,
            request,
            HttpStatusCode.NoContent,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<ImportResultDto>> ImportCustomers(byte[] fileContent, string fileName, IProgress<long>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileContent);
        ArgumentNullException.ThrowIfNull(fileName);

        return await SendImport(
            new Uri($"{RoutePrefix}/import", UriKind.Relative),
            fileContent,
            fileName,
            conflictResolutions: null,
            progress,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<ImportResultDto>> CommitImportWithResolutions(
        byte[] fileContent,
        string fileName,
        IReadOnlyDictionary<string, string> conflictResolutions,
        IProgress<long>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fileContent);
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(conflictResolutions);

        return await SendImport(
            new Uri($"{RoutePrefix}/import/commit", UriKind.Relative),
            fileContent,
            fileName,
            conflictResolutions,
            progress,
            ct).ConfigureAwait(false);
    }

    private async Task<HttpOutcome<ImportResultDto>> SendImport(
        Uri requestUri,
        byte[] fileContent,
        string fileName,
        IReadOnlyDictionary<string, string>? conflictResolutions,
        IProgress<long>? progress,
        CancellationToken ct)
    {
        HttpContent fileBytes = progress is null
            ? new ByteArrayContent(fileContent)
            : new ProgressStreamContent(new MemoryStream(fileContent, writable: false), fileContent.LongLength, progress);
        fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MediaTypeNames.Text.Csv);
        var content = new MultipartFormDataContent();
        content.Add(fileBytes, "file", fileName);
        if (conflictResolutions is not null)
        {
            content.Add(new StringContent(ConflictResolutionSerialization.Serialize(conflictResolutions)), "conflictResolutions");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
        return await HttpOutcomeMapper.Send(
            httpClient,
            request,
            HttpStatusCode.OK,
            Json.ImportResultDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    private void RecordOutcome(Activity? activity, HttpOutcome outcome)
    {
        if (outcome.StatusCode is not null)
        {
            activity?.SetTag(AdminContractsClientTelemetry.StatusCodeTag, (int)outcome.StatusCode.Value);
        }

        activity?.SetTag(AdminContractsClientTelemetry.CommandOutcomeKindTag, outcome.Kind.ToString());
        if (outcome.Kind != HttpOutcomeKind.Succeeded)
        {
            LogCustomerCreateOutcome(logger, outcome.StatusCode, outcome.Kind);
        }
    }

    [LoggerMessage(1, LogLevel.Warning, "Customer create returned {StatusCode} with outcome {OutcomeKind}.")]
    private static partial void LogCustomerCreateOutcome(ILogger logger, HttpStatusCode? statusCode, HttpOutcomeKind outcomeKind);

    /// <inheritdoc />
    public Task<HttpOutcome<ImportResultDto>> ImportCustomers(byte[] fileContent, string fileName, CancellationToken ct)
    {
        return ImportCustomers(fileContent, fileName, null, ct);
    }

    /// <inheritdoc />
    public Task<HttpOutcome<ImportResultDto>> CommitImportWithResolutions(byte[] fileContent, string fileName, IReadOnlyDictionary<string, string> conflictResolutions, CancellationToken ct)
    {
        return CommitImportWithResolutions(fileContent, fileName, conflictResolutions, null, ct);
    }
}
