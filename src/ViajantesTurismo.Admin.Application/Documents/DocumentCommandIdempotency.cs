using SharedKernel.Idempotency;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Coordinates durable ownership and replay for document commands that return a document identifier.</summary>
public sealed class DocumentCommandIdempotency(
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan ProcessingLockDuration = TimeSpan.FromMinutes(5);

    /// <summary>Gets the replay or conflict for an existing operation.</summary>
    /// <param name="scope">The resource-specific operation scope.</param>
    /// <param name="key">The optional caller-supplied idempotency key.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The existing result, or <see langword="null" /> when the caller may attempt ownership.</returns>
    public async ValueTask<Result<Guid>?> GetExistingResult(
        IdempotencyScope scope,
        IdempotencyKey? key,
        CancellationToken ct)
    {
        if (key is null)
        {
            return null;
        }

        var entry = await idempotencyStore.Get(new IdempotencyOperation(scope, key.Value), ct);
        if (entry is null)
        {
            return null;
        }

        if (entry is
            {
                State: IdempotencyEntryState.Completed,
                ResultFingerprint: { } resultFingerprint,
            }
            && Guid.TryParseExact(resultFingerprint, "N", out var documentId))
        {
            return Result.Ok(documentId);
        }

        if (entry.State == IdempotencyEntryState.Started
            && entry.StartedAt + ProcessingLockDuration <= timeProvider.GetUtcNow())
        {
            return null;
        }

        return DocumentErrors.DocumentRevisionAlreadyExists().ConvertError<Guid>();
    }

    /// <summary>Executes a document command once per optional idempotency key.</summary>
    /// <param name="scope">The resource-specific operation scope.</param>
    /// <param name="key">The optional caller-supplied idempotency key.</param>
    /// <param name="operation">The document command to execute when ownership is acquired.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The created document identifier, a completed replay, or a conflict.</returns>
    public async Task<Result<Guid>> Execute(
        IdempotencyScope scope,
        IdempotencyKey? key,
        Func<Task<Result<Guid>>> operation,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (key is null)
        {
            var result = await operation();
            if (result.IsSuccess)
            {
                await unitOfWork.SaveEntities(ct);
            }

            return result;
        }

        var idempotencyOperation = new IdempotencyOperation(scope, key.Value);
        var startResult = await idempotencyStore.TryStart(
            idempotencyOperation,
            timeProvider.GetUtcNow(),
            ProcessingLockDuration,
            ct);
        if (!startResult.Started)
        {
            if (startResult.ExistingEntry is
                {
                    State: IdempotencyEntryState.Completed,
                    ResultFingerprint: { } resultFingerprint,
                }
                && Guid.TryParseExact(resultFingerprint, "N", out var replayedDocumentId))
            {
                return Result.Ok(replayedDocumentId);
            }

            return DocumentErrors.DocumentRevisionAlreadyExists().ConvertError<Guid>();
        }

        var ownedResult = await operation();
        if (ownedResult.IsSuccess)
        {
            await idempotencyStore.StageCompletion(
                idempotencyOperation,
                timeProvider.GetUtcNow(),
                ownedResult.Value.ToString("N"),
                ct);
            await unitOfWork.SaveEntities(ct);
        }

        return ownedResult;
    }
}
