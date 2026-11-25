using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>
/// HTTP client for the Admin tours API.
/// </summary>
public sealed partial class ToursApiClient(HttpClient httpClient, ILogger<ToursApiClient>? logger = null) : IToursApiClient
{
    private const string RoutePrefix = "/api/v1/tours";
    private static readonly ToursApiClientJsonContext Json = ToursApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<HttpOutcome<GetTourPageDto>> GetTourPage(int page, TourPageSort sort, bool descending, CancellationToken ct)
    {
        var route = FormattableString.Invariant($"{RoutePrefix}/page?page={page}&sort={sort}&descending={descending}");
        return await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri(route, UriKind.Relative)),
            HttpStatusCode.OK,
            Json.GetTourPageDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<GetTourOptionsDto>> SearchTourOptions(TourOptionSearchDto request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = new HttpRequestMessage(HttpMethod.Post, new Uri($"{RoutePrefix}/options/search", UriKind.Relative))
        {
            Content = JsonContent.Create(request, Json.TourOptionSearchDto)
        };
        return await HttpOutcomeMapper.Send(
            httpClient,
            message,
            HttpStatusCode.OK,
            Json.GetTourOptionsDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<GetTourDto>> GetTourById(Guid id, CancellationToken ct)
    {
        return await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/{id}", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.GetTourDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> CreateTour(CreateTourDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var activity = AdminContractsClientTelemetry.ActivitySource.StartActivity(
            AdminContractsClientTelemetry.CreateTourActivity,
            ActivityKind.Client);
        activity?.SetTag(AdminContractsClientTelemetry.ApiAreaTag, AdminContractsClientTelemetry.AdminApiArea);
        activity?.SetTag(AdminContractsClientTelemetry.OperationTag, AdminContractsClientTelemetry.CreateTourActivity);

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(RoutePrefix, UriKind.Relative))
        {
            Content = JsonContent.Create(dto, Json.CreateTourDto)
        };
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            request,
            HttpStatusCode.Created,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);

        if (outcome.StatusCode is not null)
        {
            activity?.SetTag(AdminContractsClientTelemetry.StatusCodeTag, (int)outcome.StatusCode.Value);
        }

        activity?.SetTag(AdminContractsClientTelemetry.CommandOutcomeKindTag, outcome.Kind.ToString());
        if (outcome.Kind != HttpOutcomeKind.Succeeded && logger is not null)
        {
            LogTourCreateOutcome(logger, outcome.StatusCode, outcome.Kind);
        }

        return outcome;
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> UpdateTour(Guid id, UpdateTourDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var request = new HttpRequestMessage(HttpMethod.Put, new Uri($"{RoutePrefix}/{id}", UriKind.Relative))
        {
            Content = JsonContent.Create(dto, Json.UpdateTourDto)
        };
        return await HttpOutcomeMapper.Send(
            httpClient,
            request,
            HttpStatusCode.NoContent,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    [LoggerMessage(1, LogLevel.Warning, "Tour create returned {StatusCode} with outcome {OutcomeKind}.")]
    private static partial void LogTourCreateOutcome(ILogger logger, HttpStatusCode? statusCode, HttpOutcomeKind outcomeKind);
}
