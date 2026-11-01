using PandaBear.Idempotency;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Shares account loading, locking and staged encrypted persistence across lifecycle commands.</summary>
public sealed class PaymentAccountCommandExecutor(IPaymentAccountStore store, PaymentCommandExecutor commands)
{
    /// <summary>Applies one revision-preconditioned domain mutation under the account lock.</summary>
    public Task<Result<Guid>> Change(Guid id, string operation, IdempotencyKey key, string fingerprint, Func<PaymentAccount, Result> mutate, CancellationToken ct) =>
        commands.Execute("payment-account:" + operation + ":" + id.ToString("N"), key, fingerprint, ["admin:payment-account:" + id.ToString("N")], async () =>
        {
            var account = await store.GetById(id, ct);
            if (account is null)
            {
                return Result.NotFound(detail: "Payment account not found.").ConvertError<Guid>();
            }
            var result = mutate(account);
            if (result.IsFailure)
            {
                return result.ConvertError<Guid>();
            }
            await store.Save(account, ct);
            return Result.Ok(account.Id);
        }, ct);
}
