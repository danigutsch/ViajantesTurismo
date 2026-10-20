using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>
/// HTTP client for the Admin bookings API.
/// </summary>
public sealed partial class BookingsApiClient(HttpClient httpClient, ILogger<BookingsApiClient>? logger = null) : IBookingsApiClient
{
    private const string RoutePrefix = "/api/v1/bookings";
    private static readonly BookingsApiClientJsonContext Json = BookingsApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<HttpOutcome<GetBookingDto[]>> GetAllBookings(CancellationToken ct) =>
        await ReadBookings(RoutePrefix, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<HttpOutcome<GetBookingDto>> GetBookingById(Guid id, CancellationToken ct)
    {
        return await ContractCommandOutcome.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/{id}", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.GetBookingDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<GetBookingDto[]>> GetBookingsByTourId(Guid tourId, CancellationToken ct) =>
        await ReadBookings($"{RoutePrefix}/tour/{tourId}", ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<HttpOutcome<GetBookingDto[]>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) =>
        await ReadBookings($"{RoutePrefix}/customer/{customerId}", ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<HttpOutcome> CreateBooking(CreateBookingDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var activity = StartActivity(AdminContractsClientTelemetry.CreateBookingActivity);
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(RoutePrefix, UriKind.Relative))
        {
            Content = JsonContent.Create(dto, Json.CreateBookingDto)
        };
        var outcome = await ContractCommandOutcome.Send(
            httpClient,
            request,
            HttpStatusCode.Created,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);

        RecordOutcome(activity, outcome, LogBookingCreateOutcome);
        return outcome;
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> UpdateBookingDiscount(Guid id, UpdateBookingDiscountDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return await SendCommand(
            HttpMethod.Put,
            $"{RoutePrefix}/{id}/discount",
            JsonContent.Create(dto, Json.UpdateBookingDiscountDto),
            HttpStatusCode.OK,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> UpdateBookingDetails(Guid id, UpdateBookingDetailsDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return await SendCommand(
            HttpMethod.Put,
            $"{RoutePrefix}/{id}/details",
            JsonContent.Create(dto, Json.UpdateBookingDetailsDto),
            HttpStatusCode.OK,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> UpdateBookingNotes(Guid id, UpdateBookingNotesDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return await SendCommand(
            HttpMethod.Patch,
            $"{RoutePrefix}/{id}/notes",
            JsonContent.Create(dto, Json.UpdateBookingNotesDto),
            HttpStatusCode.OK,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> CancelBooking(Guid id, CancellationToken ct) =>
        await SendCommand(HttpMethod.Post, $"{RoutePrefix}/{id}/cancel", content: null, HttpStatusCode.OK, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<HttpOutcome> ConfirmBooking(Guid id, CancellationToken ct) =>
        await SendCommand(HttpMethod.Post, $"{RoutePrefix}/{id}/confirm", content: null, HttpStatusCode.OK, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<HttpOutcome> CompleteBooking(Guid id, CancellationToken ct) =>
        await SendCommand(HttpMethod.Post, $"{RoutePrefix}/{id}/complete", content: null, HttpStatusCode.OK, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<HttpOutcome> DeleteBooking(Guid id, CancellationToken ct) =>
        await SendCommand(HttpMethod.Delete, $"{RoutePrefix}/{id}", content: null, HttpStatusCode.NoContent, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<HttpOutcome> RecordPayment(Guid bookingId, CreatePaymentDto dto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        using var activity = StartActivity(AdminContractsClientTelemetry.RecordPaymentActivity);
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"{RoutePrefix}/{bookingId}/payments", UriKind.Relative))
        {
            Content = JsonContent.Create(dto, Json.CreatePaymentDto)
        };
        var outcome = await ContractCommandOutcome.Send(
            httpClient,
            request,
            HttpStatusCode.Created,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);

        RecordOutcome(activity, outcome, LogRecordPaymentOutcome);
        return outcome;
    }

    private async Task<HttpOutcome<GetBookingDto[]>> ReadBookings(string requestUri, CancellationToken ct)
    {
        var outcome = await ContractCommandOutcome.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri(requestUri, UriKind.Relative)),
            HttpStatusCode.OK,
            Json.GetBookingDtoArray,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);

        return outcome.Kind == ContractCommandOutcomeKind.Succeeded
            ? outcome with { Value = outcome.Value?.OfType<GetBookingDto>().ToArray() ?? [] }
            : outcome;
    }

    private async Task<HttpOutcome> SendCommand(
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        HttpStatusCode expectedStatusCode,
        CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, new Uri(requestUri, UriKind.Relative)) { Content = content };
        return await ContractCommandOutcome.Send(
            httpClient,
            request,
            expectedStatusCode,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    private static Activity? StartActivity(string operation)
    {
        var activity = AdminContractsClientTelemetry.ActivitySource.StartActivity(operation, ActivityKind.Client);
        activity?.SetTag(AdminContractsClientTelemetry.ApiAreaTag, AdminContractsClientTelemetry.AdminApiArea);
        activity?.SetTag(AdminContractsClientTelemetry.OperationTag, operation);
        return activity;
    }

    private void RecordOutcome(
        Activity? activity,
        HttpOutcome outcome,
        Action<ILogger, HttpStatusCode?, ContractCommandOutcomeKind> log)
    {
        if (outcome.StatusCode is not null)
        {
            activity?.SetTag(AdminContractsClientTelemetry.StatusCodeTag, (int)outcome.StatusCode.Value);
        }

        activity?.SetTag(AdminContractsClientTelemetry.CommandOutcomeKindTag, outcome.Kind.ToString());
        if (outcome.Kind != ContractCommandOutcomeKind.Succeeded && logger is not null)
        {
            log(logger, outcome.StatusCode, outcome.Kind);
        }
    }

    [LoggerMessage(1, LogLevel.Warning, "Booking create returned {StatusCode} with outcome {OutcomeKind}.")]
    private static partial void LogBookingCreateOutcome(ILogger logger, HttpStatusCode? statusCode, ContractCommandOutcomeKind outcomeKind);

    [LoggerMessage(2, LogLevel.Warning, "Record payment returned {StatusCode} with outcome {OutcomeKind}.")]
    private static partial void LogRecordPaymentOutcome(ILogger logger, HttpStatusCode? statusCode, ContractCommandOutcomeKind outcomeKind);
}
