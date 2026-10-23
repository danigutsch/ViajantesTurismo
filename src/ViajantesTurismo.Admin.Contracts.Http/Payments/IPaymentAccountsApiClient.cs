using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Provides masked account reads and explicit Admin recipient lifecycle operations.</summary>
public interface IPaymentAccountsApiClient
{
    /// <summary>Lists masked recipient accounts.</summary>
    Task<HttpOutcome<IReadOnlyList<GetPaymentAccountDto>>> List(CancellationToken ct);
    /// <summary>Gets a masked account.</summary>
    Task<HttpOutcome<GetPaymentAccountDto>> GetById(Guid id, CancellationToken ct);
    /// <summary>Explicitly reads sensitive recipient details, requiring Admin authorization and a server audit.</summary>
    Task<HttpOutcome<GetPaymentAccountDetailsDto>> GetSensitiveDetails(Guid id, CancellationToken ct);
    /// <summary>Executes Create with a stable idempotency key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentAccountDto>> Create(CreatePaymentAccountDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Update with a stable idempotency key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentAccountDto>> Update(Guid id, UpdatePaymentAccountDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Verify with a stable idempotency key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentAccountDto>> Verify(Guid id, VerifyPaymentAccountDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Review with a stable idempotency key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentAccountDto>> Review(Guid id, ReviewPaymentAccountDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Retire with a stable idempotency key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentAccountDto>> Retire(Guid id, RetirePaymentAccountDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes WithdrawVerification with a stable idempotency key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentAccountDto>> WithdrawVerification(Guid id, WithdrawPaymentAccountVerificationDto input, string idempotencyKey, CancellationToken ct);
}
