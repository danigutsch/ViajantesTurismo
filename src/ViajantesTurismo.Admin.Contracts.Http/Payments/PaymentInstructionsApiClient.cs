using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Implements explicit payment instruction operations through the document API.</summary>
public sealed class PaymentInstructionsApiClient(HttpClient httpClient) : IPaymentInstructionsApiClient
{
    private const string Prefix = "/api/v1/documents";
    private static readonly PaymentFinancialContractJsonContext Json = PaymentFinancialContractJsonContext.Default;
    private static JsonTypeInfo<ContractValidationProblemDto> Validation => PaymentHttpProblemJsonContext.Default.ContractValidationProblemDto;
    /// <inheritdoc />
    public Task<HttpOutcome<IReadOnlyList<GetPaymentInstructionDto>>> ListForBooking(Guid bookingId, CancellationToken ct) => HttpOutcomeMapper.Send(httpClient,
        new HttpRequestMessage(HttpMethod.Get, new Uri($"{Prefix}/bookings/{bookingId}/payment-instructions", UriKind.Relative)), HttpStatusCode.OK, Json.IReadOnlyListGetPaymentInstructionDto, Validation, ct);
    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentInstructionDto>> GetById(Guid id, CancellationToken ct) => HttpOutcomeMapper.Send(httpClient,
        new HttpRequestMessage(HttpMethod.Get, new Uri($"{Prefix}/{id}/payment-instructions", UriKind.Relative)), HttpStatusCode.OK, Json.GetPaymentInstructionDto, Validation, ct);
    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentInstructionDto>> Generate(Guid bookingId, GeneratePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/bookings/{bookingId}/payment-instructions", input, Json.GeneratePaymentInstructionsDto, idempotencyKey, HttpStatusCode.Created, ct);
    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentInstructionDto>> Regenerate(Guid bookingId, Guid id, GeneratePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/bookings/{bookingId}/payment-instructions/{id}/regenerate", input, Json.GeneratePaymentInstructionsDto, idempotencyKey, HttpStatusCode.Created, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentInstructionDto>> Approve(Guid id, UpdatePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{id}/payment-instructions/approve", input, Json.UpdatePaymentInstructionsDto, idempotencyKey, HttpStatusCode.OK, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentInstructionDto>> Finalize(Guid id, UpdatePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{id}/payment-instructions/finalize", input, Json.UpdatePaymentInstructionsDto, idempotencyKey, HttpStatusCode.OK, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentInstructionDto>> WithdrawApproval(Guid id, UpdatePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{id}/payment-instructions/withdraw-approval", input, Json.UpdatePaymentInstructionsDto, idempotencyKey, HttpStatusCode.OK, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentInstructionDto>> Void(Guid id, UpdatePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{id}/payment-instructions/void", input, Json.UpdatePaymentInstructionsDto, idempotencyKey, HttpStatusCode.OK, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentInstructionDto>> Review(Guid id, ReviewPaymentInstructionsDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{id}/payment-instructions/review", input, Json.ReviewPaymentInstructionsDto, idempotencyKey, HttpStatusCode.OK, ct);

    private Task<HttpOutcome<GetPaymentInstructionDto>> Send<T>(string route, T input, JsonTypeInfo<T> type, string key, HttpStatusCode expected, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(route, UriKind.Relative)) { Content = JsonContent.Create(input, type) };
        request.Headers.Add(ContractConstants.IdempotencyKeyHeaderName, key);
        return HttpOutcomeMapper.Send(httpClient, request, expected, Json.GetPaymentInstructionDto, Validation, ct);
    }
}
