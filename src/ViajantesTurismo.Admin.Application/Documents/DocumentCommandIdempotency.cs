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

    /// <summary>Creates the validation error for a required document operation key.</summary>
    /// <returns>The stable required-key validation problem.</returns>
    public static Result RequiredKeyError() => Result.Invalid(
        detail: "Idempotency-Key is required.",
        field: "Idempotency-Key",
        message: "Idempotency-Key is required.");

    /// <summary>Runs generation preparation and its deferred durable mutation.</summary>
    /// <param name="command">The command defining resource scope and meaningful template inputs.</param>
    /// <param name="auditWriter">The writer for rejected ownership audits.</param>
    /// <param name="prepare">Prepares source data before invoking the commit continuation.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The command result, replay, or audited conflict.</returns>
    public Task<Result<Guid>> Execute(
        GenerateContractDraftCommand command,
        DocumentAuditWriter auditWriter,
        DocumentCommandPreparation prepare,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ExecutePrepared(
            command.Scope, command.IdempotencyKey, command.TemplateId, command.TemplateVersion,
            command.AuditContext, DocumentAuditOperation.Generate, null, command.BookingId,
            auditWriter, prepare, ct);
    }

    /// <summary>Runs regeneration preparation and its deferred durable mutation.</summary>
    /// <param name="command">The command defining resource scope and meaningful template inputs.</param>
    /// <param name="auditWriter">The writer for rejected ownership audits.</param>
    /// <param name="prepare">Prepares source data before invoking the commit continuation.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The command result, replay, or audited conflict.</returns>
    public Task<Result<Guid>> Execute(
        RegenerateDocumentDraftCommand command,
        DocumentAuditWriter auditWriter,
        DocumentCommandPreparation prepare,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ExecutePrepared(
            command.Scope, command.IdempotencyKey, command.TemplateId, command.TemplateVersion,
            command.AuditContext, DocumentAuditOperation.Regenerate, command.DocumentId, null,
            auditWriter, prepare, ct);
    }

    private async Task<Result<Guid>> ExecutePrepared(
        IdempotencyScope scope,
        IdempotencyKey key,
        string templateId,
        string templateVersion,
        DocumentAuditContext auditContext,
        DocumentAuditOperation auditOperation,
        Guid? documentId,
        Guid? bookingId,
        DocumentAuditWriter auditWriter,
        DocumentCommandPreparation prepare,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(auditWriter);
        ArgumentNullException.ThrowIfNull(prepare);
        if (key == default)
        {
            return RequiredKeyError().ConvertError<Guid>();
        }

        if (auditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired().ConvertError<Guid>();
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateVersion);
        var requestFingerprint = IdempotencyRequestFingerprint.Create(templateId, templateVersion);
        var existingResult = await GetExistingResult(scope, key, requestFingerprint, ct);
        if (existingResult is { IsSuccess: true } replayedResult)
        {
            return replayedResult;
        }

        if (existingResult is { } conflictResult)
        {
            return await RecordConflict(
                conflictResult, auditWriter, auditContext, auditOperation, documentId, bookingId, null, ct);
        }

        return await prepare(async (operation, preparedDocumentId, preparedBookingId, preparedRevision) =>
        {
            var operationWasInvoked = false;
            var result = await Execute(scope, key, requestFingerprint, () =>
            {
                operationWasInvoked = true;
                return operation();
            }, ct);
            return result.IsFailure && !operationWasInvoked
                ? await RecordConflict(
                    result, auditWriter, auditContext, auditOperation,
                    preparedDocumentId, preparedBookingId, preparedRevision, ct)
                : result;
        });
    }

    private static async Task<Result<Guid>> RecordConflict(
        Result<Guid> result,
        DocumentAuditWriter auditWriter,
        DocumentAuditContext auditContext,
        DocumentAuditOperation operation,
        Guid? documentId,
        Guid? bookingId,
        int? revision,
        CancellationToken ct)
    {
        var auditResult = await auditWriter.Add(
            auditContext, operation, documentId, bookingId, revision,
            DocumentAuditOutcome.Rejected, DocumentAuditReasonCode.StateConflict, ct);
        return auditResult.IsFailure ? auditResult.ConvertError<Guid>() : result;
    }

    /// <summary>Gets the replay or conflict for an existing operation.</summary>
    /// <param name="scope">The resource-specific operation scope.</param>
    /// <param name="key">The required caller-supplied idempotency key.</param>
    /// <param name="requestFingerprint">The privacy-safe fingerprint of the operation input.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The existing result, or <see langword="null" /> when the caller may attempt ownership.</returns>
    public async ValueTask<Result<Guid>?> GetExistingResult(
        IdempotencyScope scope,
        IdempotencyKey key,
        string requestFingerprint,
        CancellationToken ct)
    {
        if (key == default)
        {
            return RequiredKeyError().ConvertError<Guid>();
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(requestFingerprint);

        var entry = await idempotencyStore.Get(new IdempotencyOperation(scope, key), ct);
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

    /// <summary>Executes a document command once per required idempotency key.</summary>
    /// <param name="scope">The resource-specific operation scope.</param>
    /// <param name="key">The required caller-supplied idempotency key.</param>
    /// <param name="requestFingerprint">The privacy-safe fingerprint of the operation input.</param>
    /// <param name="operation">The document command to execute when ownership is acquired.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The created document identifier, a completed replay, or a conflict.</returns>
    public async Task<Result<Guid>> Execute(
        IdempotencyScope scope,
        IdempotencyKey key,
        string requestFingerprint,
        Func<Task<Result<Guid>>> operation,
        CancellationToken ct)
    {
        if (key == default)
        {
            return RequiredKeyError().ConvertError<Guid>();
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(requestFingerprint);
        ArgumentNullException.ThrowIfNull(operation);

        var idempotencyOperation = new IdempotencyOperation(scope, key);
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
