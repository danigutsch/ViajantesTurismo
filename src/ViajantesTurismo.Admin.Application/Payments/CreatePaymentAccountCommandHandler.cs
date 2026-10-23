using ViajantesTurismo.Admin.Application.Idempotency;
using System.Text.Json;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Saves an unverified account draft and its metadata audit atomically.</summary>
public sealed class CreatePaymentAccountCommandHandler(IPaymentAccountStore store, PaymentCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Creates a draft; an explicit verification command is always required afterwards.</summary>
    public async Task<Result<Guid>> Handle(CreatePaymentAccountCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (input.Recipient is null)
        {
            return PaymentAccountErrors.Invalid("recipient").ConvertError<Guid>();
        }
        var recipient = PaymentAccountMappings.ToRecipient(input.Recipient);
        if (recipient.IsFailure)
        {
            return recipient.ConvertError<PaymentRecipientDetails, Guid>();
        }
        var fingerprint = ApplicationRequestFingerprint.Create(command.ActorId, JsonSerializer.Serialize(input, PaymentAccountContractJsonContext.Default.CreatePaymentAccountDto));
        return await executor.Execute("payment-account:create", command.IdempotencyKey, fingerprint, [], async () =>
        {
            var account = PaymentAccount.Create(input.Label, recipient.Value, command.ActorId, clock.GetUtcNow());
            if (account.IsFailure)
            {
                return account.ConvertError<PaymentAccount, Guid>();
            }
            await store.Save(account.Value, ct);
            return Result.Ok(account.Value.Id);
        }, ct);
    }
}
