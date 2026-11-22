using PandaBear.Idempotency;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Shares account loading and staged encrypted persistence across lifecycle commands.</summary>
public sealed class PaymentAccountOperations(IPaymentAccountStore store, PaymentCommandIdempotency idempotency)
{
    /// <summary>Applies one revision-preconditioned domain mutation inside the command's transaction.</summary>
    public ValueTask<Result<Guid>> Change(Guid id, string scope, IdempotencyKey key, string fingerprint, Func<PaymentAccount, Result> mutate, CancellationToken ct) =>
        idempotency.Execute(scope, key, fingerprint, async () =>
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
