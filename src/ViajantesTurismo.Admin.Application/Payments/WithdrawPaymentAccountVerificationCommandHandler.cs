using ViajantesTurismo.Admin.Application.Idempotency;
using System.Text.Json;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the WithdrawPaymentAccountVerification lifecycle action with optimistic concurrency and durable idempotency.</summary>
public sealed class WithdrawPaymentAccountVerificationCommandHandler(PaymentAccountOperations executor, TimeProvider clock) : ICommandHandler<WithdrawPaymentAccountVerificationCommand, Result<Guid>>
{
    /// <summary>Applies the requested lifecycle change.</summary>
    public ValueTask<Result<Guid>> Handle(WithdrawPaymentAccountVerificationCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        var fingerprint = ApplicationRequestFingerprint.Create(command.AccountId.ToString("N"), command.ActorId, JsonSerializer.Serialize(input, PaymentAccountContractJsonContext.Default.WithdrawPaymentAccountVerificationDto));
        return executor.Change(command.AccountId, command.OperationScope, command.IdempotencyKey, fingerprint, account => account.WithdrawVerification(input.ExpectedVersion, command.ActorId, clock.GetUtcNow(), input.Confirmed), ct);
    }
}
