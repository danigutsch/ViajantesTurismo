using ViajantesTurismo.Admin.Application.Idempotency;
using System.Text.Json;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the RetirePaymentAccount lifecycle action with optimistic concurrency and durable idempotency.</summary>
public sealed class RetirePaymentAccountCommandHandler(PaymentAccountOperations executor, TimeProvider clock) : ICommandHandler<RetirePaymentAccountCommand, Result<Guid>>
{
    /// <summary>Applies the requested lifecycle change.</summary>
    public ValueTask<Result<Guid>> Handle(RetirePaymentAccountCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (!Enum.IsDefined(input.Reason))
        {
            return ValueTask.FromResult(PaymentAccountErrors.Invalid("Reason").ConvertError<Guid>());
        }
        var fingerprint = ApplicationRequestFingerprint.Create(command.AccountId.ToString("N"), command.ActorId, JsonSerializer.Serialize(input, PaymentAccountContractJsonContext.Default.RetirePaymentAccountDto));
        return executor.Change(command.AccountId, command.OperationScope, command.IdempotencyKey, fingerprint, account => account.Retire(input.ExpectedVersion, command.ActorId, clock.GetUtcNow(), PaymentAccountMappings.Map(input.Reason), input.Confirmed), ct);
    }
}
