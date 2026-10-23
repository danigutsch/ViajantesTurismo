using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Implements typed, payload-free payment-account HTTP operations.</summary>
public sealed class PaymentAccountsApiClient(HttpClient httpClient) : IPaymentAccountsApiClient
{
    private const string Prefix = "/api/v1/payment-accounts";
    private static readonly PaymentAccountContractJsonContext Json = PaymentAccountContractJsonContext.Default;
    private static JsonTypeInfo<ContractValidationProblemDto> Validation => PaymentHttpProblemJsonContext.Default.ContractValidationProblemDto;

    /// <inheritdoc />
    public Task<HttpOutcome<IReadOnlyList<GetPaymentAccountDto>>> List(CancellationToken ct) => HttpOutcomeMapper.Send(httpClient,
        new HttpRequestMessage(HttpMethod.Get, new Uri(Prefix, UriKind.Relative)), HttpStatusCode.OK, Json.IReadOnlyListGetPaymentAccountDto, Validation, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentAccountDto>> GetById(Guid id, CancellationToken ct) => HttpOutcomeMapper.Send(httpClient,
        new HttpRequestMessage(HttpMethod.Get, new Uri($"{Prefix}/{id}", UriKind.Relative)), HttpStatusCode.OK, Json.GetPaymentAccountDto, Validation, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentAccountDetailsDto>> GetSensitiveDetails(Guid id, CancellationToken ct) => HttpOutcomeMapper.Send(httpClient,
        new HttpRequestMessage(HttpMethod.Get, new Uri($"{Prefix}/{id}/recipient", UriKind.Relative)), HttpStatusCode.OK, Json.GetPaymentAccountDetailsDto, Validation, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentAccountDto>> Create(CreatePaymentAccountDto input, string idempotencyKey, CancellationToken ct) =>
        Send(HttpMethod.Post, $"{Prefix}", input, Json.CreatePaymentAccountDto, idempotencyKey, HttpStatusCode.Created, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentAccountDto>> Update(Guid id, UpdatePaymentAccountDto input, string idempotencyKey, CancellationToken ct) =>
        Send(HttpMethod.Put, $"{Prefix}/{id}", input, Json.UpdatePaymentAccountDto, idempotencyKey, HttpStatusCode.OK, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentAccountDto>> Verify(Guid id, VerifyPaymentAccountDto input, string idempotencyKey, CancellationToken ct) =>
        Send(HttpMethod.Post, $"{Prefix}/{id}/verify", input, Json.VerifyPaymentAccountDto, idempotencyKey, HttpStatusCode.OK, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentAccountDto>> Review(Guid id, ReviewPaymentAccountDto input, string idempotencyKey, CancellationToken ct) =>
        Send(HttpMethod.Post, $"{Prefix}/{id}/review", input, Json.ReviewPaymentAccountDto, idempotencyKey, HttpStatusCode.OK, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentAccountDto>> Retire(Guid id, RetirePaymentAccountDto input, string idempotencyKey, CancellationToken ct) =>
        Send(HttpMethod.Post, $"{Prefix}/{id}/retire", input, Json.RetirePaymentAccountDto, idempotencyKey, HttpStatusCode.OK, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentAccountDto>> WithdrawVerification(Guid id, WithdrawPaymentAccountVerificationDto input, string idempotencyKey, CancellationToken ct) =>
        Send(HttpMethod.Post, $"{Prefix}/{id}/withdraw-verification", input, Json.WithdrawPaymentAccountVerificationDto, idempotencyKey, HttpStatusCode.OK, ct);

    private Task<HttpOutcome<GetPaymentAccountDto>> Send<T>(HttpMethod method, string route, T input, JsonTypeInfo<T> type, string key, HttpStatusCode expected, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var request = new HttpRequestMessage(method, new Uri(route, UriKind.Relative)) { Content = JsonContent.Create(input, type) };
        request.Headers.Add(ContractConstants.IdempotencyKeyHeaderName, key);
        return HttpOutcomeMapper.Send(httpClient, request, expected, Json.GetPaymentAccountDto, Validation, ct);
    }
}
