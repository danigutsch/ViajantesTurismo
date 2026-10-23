using ViajantesTurismo.Admin.Application.Idempotency;
using System.Text.Json;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the VerifyPaymentAccount lifecycle action with optimistic concurrency and durable idempotency.</summary>
public sealed class VerifyPaymentAccountCommandHandler(PaymentAccountCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Applies the requested lifecycle change.</summary>
    public Task<Result<Guid>> Handle(VerifyPaymentAccountCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (!Enum.IsDefined(input.Method))
        {
            return Task.FromResult(PaymentAccountErrors.Invalid("Method").ConvertError<Guid>());
        }
        var fingerprint = ApplicationRequestFingerprint.Create(command.AccountId.ToString("N"), command.ActorId, JsonSerializer.Serialize(input, PaymentAccountContractJsonContext.Default.VerifyPaymentAccountDto));
        return executor.Change(command.AccountId, "VerifyPaymentAccount", command.IdempotencyKey, fingerprint, account => account.Verify(input.ExpectedVersion, command.ActorId, clock.GetUtcNow(), PaymentAccountMappings.Map(input.Method), input.CheckedAt, input.IndependentCheckAttested, input.Confirmed), ct);
    }
}
