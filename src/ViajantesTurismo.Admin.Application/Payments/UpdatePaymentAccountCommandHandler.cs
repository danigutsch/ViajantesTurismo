using ViajantesTurismo.Admin.Application.Idempotency;
using System.Text.Json;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the UpdatePaymentAccount lifecycle action with optimistic concurrency and durable idempotency.</summary>
public sealed class UpdatePaymentAccountCommandHandler(PaymentAccountOperations executor, TimeProvider clock) : ICommandHandler<UpdatePaymentAccountCommand, Result<Guid>>
{
    /// <summary>Applies the requested lifecycle change.</summary>
    public ValueTask<Result<Guid>> Handle(UpdatePaymentAccountCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (input.Recipient is null)
        {
            return ValueTask.FromResult(PaymentAccountErrors.Invalid("recipient").ConvertError<Guid>());
        }
        var recipient = PaymentAccountMappings.ToRecipient(input.Recipient);
        if (recipient.IsFailure)
        {
            return ValueTask.FromResult(recipient.ConvertError<PaymentRecipientDetails, Guid>());
        }
        var fingerprint = ApplicationRequestFingerprint.Create(command.AccountId.ToString("N"), command.ActorId, JsonSerializer.Serialize(input, PaymentAccountContractJsonContext.Default.UpdatePaymentAccountDto));
        return executor.Change(command.AccountId, command.OperationScope, command.IdempotencyKey, fingerprint, account => account.Revise(input.ExpectedVersion, input.Label, recipient.Value, command.ActorId, clock.GetUtcNow()), ct);
    }
}
