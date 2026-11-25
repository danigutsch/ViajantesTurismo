using SharedKernel.Idempotency;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Claims and completes a payment operation inside the mediator's transaction.</summary>
public sealed class PaymentCommandIdempotency(IIdempotencyStore idempotency, IUnitOfWork unitOfWork, TimeProvider clock)
{
    /// <summary>Replays an identical completed operation or stages a new mutation and its result together.</summary>
    public async ValueTask<Result<Guid>> Execute(string scope, IdempotencyKey key, string fingerprint, Func<Task<Result<Guid>>> mutate, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        if (key == default)
        {
            return Result.Invalid(detail: "A valid Idempotency-Key is required.", field: ContractConstants.IdempotencyKeyHeaderName, message: "A valid Idempotency-Key is required.").ConvertError<Guid>();
        }
        var operation = new IdempotencyOperation(IdempotencyScope.From(scope), key);
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
    }
}
