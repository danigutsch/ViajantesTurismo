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
    private const char FingerprintSeparator = ':';
    private const int GuidNLength = 32;
    private const int Sha256HexLength = 64;
    private static readonly TimeSpan ProcessingLockDuration = TimeSpan.FromMinutes(5);

    /// <summary>Creates a privacy-safe fingerprint for the template inputs bound to an idempotency key.</summary>
    /// <param name="templateId">The server-selected template identifier.</param>
    /// <param name="templateVersion">The server-selected template version.</param>
    /// <returns>An uppercase SHA-256 fingerprint that contains no raw template input.</returns>
    public static string CreateRequestFingerprint(string templateId, string templateVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateVersion);

        return IdempotencyRequestFingerprint.Create(templateId, templateVersion);
    }

    /// <summary>Gets the replay or conflict for an existing operation.</summary>
    /// <param name="scope">The resource-specific operation scope.</param>
    /// <param name="key">The optional caller-supplied idempotency key.</param>
    /// <param name="requestFingerprint">The privacy-safe fingerprint of the operation input.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The existing result, or <see langword="null" /> when the caller may attempt ownership.</returns>
    public async ValueTask<Result<Guid>?> GetExistingResult(
        IdempotencyScope scope,
        IdempotencyKey? key,
        string requestFingerprint,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFingerprint);

        if (key is null)
        {
            return null;
        }

        var entry = await idempotencyStore.Get(new IdempotencyOperation(scope, key.Value), ct);
        if (entry is null)
        {
            return null;
        }

        if (TryGetCompletedDocumentId(entry, requestFingerprint, out var documentId))
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
    /// <param name="requestFingerprint">The privacy-safe fingerprint of the operation input.</param>
    /// <param name="operation">The document command to execute when ownership is acquired.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The created document identifier, a completed replay, or a conflict.</returns>
    public async Task<Result<Guid>> Execute(
        IdempotencyScope scope,
        IdempotencyKey? key,
        string requestFingerprint,
        Func<Task<Result<Guid>>> operation,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFingerprint);
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
            if (TryGetCompletedDocumentId(startResult.ExistingEntry, requestFingerprint, out var replayedDocumentId))
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
                CreateResultFingerprint(requestFingerprint, ownedResult.Value),
                ct);
            await unitOfWork.SaveEntities(ct);
        }

        return ownedResult;
    }

    private static string CreateResultFingerprint(string requestFingerprint, Guid documentId) =>
        string.Concat(requestFingerprint, FingerprintSeparator, documentId.ToString("N"));

    private static bool TryGetCompletedDocumentId(
        IdempotencyEntry? entry,
        string requestFingerprint,
        out Guid documentId)
    {
        documentId = Guid.Empty;
        if (entry is not
            {
                State: IdempotencyEntryState.Completed,
                ResultFingerprint: { } resultFingerprint,
            })
        {
            return false;
        }

        return resultFingerprint.Length == Sha256HexLength + 1 + GuidNLength
            && resultFingerprint[Sha256HexLength] == FingerprintSeparator
            && resultFingerprint.AsSpan(0, Sha256HexLength).Equals(
                requestFingerprint.AsSpan(),
                StringComparison.Ordinal)
            && Guid.TryParseExact(resultFingerprint.AsSpan((Sha256HexLength + 1)..), "N", out documentId);
    }
}
