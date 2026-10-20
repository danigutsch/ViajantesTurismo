using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
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
    public async Task<HttpOutcome<GetTourDto[]>> GetTours(CancellationToken cancellationToken, int maxItems = int.MaxValue)
    {
        if (maxItems <= 0)
        {
            return ContractCommandOutcome.Succeeded(HttpStatusCode.OK, Array.Empty<GetTourDto>());
        }

        var outcome = await ContractCommandOutcome.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri(RoutePrefix, UriKind.Relative)),
            HttpStatusCode.OK,
            Json.GetTourDtoArray,
            Json.ContractValidationProblemDto,
            cancellationToken).ConfigureAwait(false);

        return outcome.Kind == ContractCommandOutcomeKind.Succeeded
            ? outcome with { Value = outcome.Value?.OfType<GetTourDto>().Take(maxItems).ToArray() ?? [] }
            : outcome;
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<GetTourDto>> GetTourById(Guid id, CancellationToken cancellationToken)
    {
        return await ContractCommandOutcome.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/{id}", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.GetTourDto,
            Json.ContractValidationProblemDto,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> CreateTour(CreateTourDto dto, CancellationToken cancellationToken)
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
        var outcome = await ContractCommandOutcome.Send(
            httpClient,
            request,
            HttpStatusCode.Created,
            Json.ContractValidationProblemDto,
            cancellationToken).ConfigureAwait(false);

        if (outcome.StatusCode is not null)
        {
            activity?.SetTag(AdminContractsClientTelemetry.StatusCodeTag, (int)outcome.StatusCode.Value);
        }

        activity?.SetTag(AdminContractsClientTelemetry.CommandOutcomeKindTag, outcome.Kind.ToString());
        if (outcome.Kind != ContractCommandOutcomeKind.Succeeded && logger is not null)
        {
            LogTourCreateOutcome(logger, outcome.StatusCode, outcome.Kind);
        }

        return outcome;
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> UpdateTour(Guid id, UpdateTourDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var request = new HttpRequestMessage(HttpMethod.Put, new Uri($"{RoutePrefix}/{id}", UriKind.Relative))
        {
            Content = JsonContent.Create(dto, Json.UpdateTourDto)
        };
        return await ContractCommandOutcome.Send(
            httpClient,
            request,
            HttpStatusCode.NoContent,
            Json.ContractValidationProblemDto,
            cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(1, LogLevel.Warning, "Tour create returned {StatusCode} with outcome {OutcomeKind}.")]
    private static partial void LogTourCreateOutcome(ILogger logger, HttpStatusCode? statusCode, ContractCommandOutcomeKind outcomeKind);
}
