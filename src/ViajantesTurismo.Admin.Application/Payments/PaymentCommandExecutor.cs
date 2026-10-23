using ViajantesTurismo.Admin.Application.Idempotency;
using ViajantesTurismo.Admin.Contracts.Application;
using PandaBear.Idempotency;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Commits payment mutations, metadata audits and changed-payload-safe idempotency atomically.</summary>
public sealed class PaymentCommandExecutor(IPaymentTransaction transaction, IIdempotencyStore idempotency, IUnitOfWork unitOfWork, TimeProvider clock)
{
    /// <summary>Executes a canonically fingerprinted financial operation under its resource and idempotency locks.</summary>
    public async Task<Result<Guid>> Execute(string scope, string? key, string fingerprint, IReadOnlyList<string> resources, Func<Task<Result<Guid>>> mutate, CancellationToken ct)
    {
        if (!IdempotencyKey.TryFrom(key, out var parsedKey))
        {
            return Result.Invalid(detail: "A valid Idempotency-Key is required.", field: ContractConstants.IdempotencyKeyHeaderName, message: "A valid Idempotency-Key is required.").ConvertError<Guid>();
        }
        var operation = new IdempotencyOperation(IdempotencyScope.From(scope), parsedKey);
        var lockNames = resources.Append("admin:payment-operation:" + ApplicationRequestFingerprint.Create(scope, parsedKey.Value)).ToArray();
        return await transaction.Execute(lockNames, async () =>
        {
            var start = await idempotency.TryStart(operation, clock.GetUtcNow(), TimeSpan.FromMinutes(5), ct);
            if (!start.Started)
            {
                var stored = start.ExistingEntry;
                if (stored is { State: IdempotencyEntryState.Completed, ResultFingerprint: { } value }
                    && value.Length == 97 && value.StartsWith(fingerprint + ":", StringComparison.Ordinal)
                    && Guid.TryParseExact(value.AsSpan(65), "N", out var replayId))
                {
                    return Result.Ok(replayId);
                }
                return Result.Conflict(detail: "The idempotency key is already in use or its command inputs differ.").ConvertError<Guid>();
            }
            var result = await mutate();
            if (result.IsFailure)
            {
                return result;
            }
            await idempotency.StageCompletion(operation, clock.GetUtcNow(), fingerprint + ":" + result.Value.ToString("N"), ct);
            await unitOfWork.SaveEntities(ct);
            return result;
        }, ct);
    }
}
