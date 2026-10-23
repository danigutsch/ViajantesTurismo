using ViajantesTurismo.Admin.Application.Idempotency;
using System.Text.Json;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Application.Mappings;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the ReviewPaymentAccount lifecycle action with optimistic concurrency and durable idempotency.</summary>
public sealed class ReviewPaymentAccountCommandHandler(PaymentAccountCommandExecutor executor, TimeProvider clock)
{
    /// <summary>Applies the requested lifecycle change.</summary>
    public Task<Result<Guid>> Handle(ReviewPaymentAccountCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var input = command.Input;
        if (!Enum.IsDefined(input.Outcome))
        {
            return Task.FromResult(PaymentAccountErrors.Invalid("Outcome").ConvertError<Guid>());
        }
        var fingerprint = ApplicationRequestFingerprint.Create(command.AccountId.ToString("N"), command.ActorId, JsonSerializer.Serialize(input, PaymentAccountContractJsonContext.Default.ReviewPaymentAccountDto));
        return executor.Change(command.AccountId, "ReviewPaymentAccount", command.IdempotencyKey, fingerprint, account => account.Review(input.ExpectedVersion, command.ActorId, clock.GetUtcNow(), PaymentAccountMappings.Map(input.Outcome)), ct);
    }
}
