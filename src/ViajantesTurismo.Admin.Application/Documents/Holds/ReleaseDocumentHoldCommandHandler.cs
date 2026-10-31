using PandaBear.DispositionHold;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>
/// Releases a disposition hold once, recording the releasing actor and reason code, and records the release or the
/// rejection in the document audit trail.
/// </summary>
public sealed class ReleaseDocumentHoldCommandHandler(
    IDocumentHoldQuery holdQuery,
    IDispositionHoldStore holdStore,
    IDocumentDispositionTransaction transaction,
    TimeProvider timeProvider,
    DocumentAuditWriter auditWriter)
{
    private const string ReleasingActorParameter = "releasedBy";
    private const string ReleaseLockPrefix = "admin:document-hold-release:";

    /// <summary>Releases the hold and returns it; a second release is a conflict and a foreign hold is not found.</summary>
    public async Task<Result<Hold>> Handle(ReleaseDocumentHoldCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.AuditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired().ConvertError<Hold>();
        }

        var lineageId = await holdQuery.FindLineageId(command.DocumentId, ct);
        if (lineageId is null)
        {
            return await Reject(DocumentErrors.DocumentNotFound().ConvertError<Hold>(), command, null, DocumentAuditReasonCode.DocumentNotFound, ct);
        }

        var lockName = string.Concat(ReleaseLockPrefix, command.HoldId.ToString("N"));
        return await transaction.Execute([lockName], () => Release(command, DocumentHoldScopes.ForLineage(lineageId.Value), ct), ct);
    }

    private async Task<Result<Hold>> Release(ReleaseDocumentHoldCommand command, DispositionScope scope, CancellationToken ct)
    {
        var hold = await holdStore.Find(command.HoldId, ct);
        if (hold is null || hold.Scope != scope)
        {
            return await Reject(DocumentHoldErrors.HoldNotFound().ConvertError<Hold>(), command, null, DocumentAuditReasonCode.HoldNotFound, ct);
        }

        if (hold.IsReleased)
        {
            return await Reject(DocumentHoldErrors.HoldAlreadyReleased().ConvertError<Hold>(), command, hold, DocumentAuditReasonCode.StateConflict, ct);
        }

        var released = TryRelease(hold, command);
        if (released.IsFailure)
        {
            return await Reject(released, command, hold, DocumentAuditReasonCode.ValidationRejected, ct);
        }

        var audit = await auditWriter.AddHold(
            command.AuditContext,
            DocumentAuditOperation.ReleaseHold,
            command.DocumentId,
            hold.Id,
            hold.Basis.ToString(),
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualHoldRelease,
            ct);
        return audit.IsFailure ? audit.ConvertError<Hold>() : released;
    }

    private async Task<Result<Hold>> Reject(
        Result<Hold> rejection,
        ReleaseDocumentHoldCommand command,
        Hold? hold,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct)
    {
        var audit = await auditWriter.AddHold(
            command.AuditContext,
            DocumentAuditOperation.ReleaseHold,
            command.DocumentId,
            command.HoldId,
            hold?.Basis.ToString(),
            DocumentAuditOutcome.Rejected,
            reasonCode,
            ct);
        return audit.IsFailure ? audit.ConvertError<Hold>() : rejection;
    }

    private Result<Hold> TryRelease(Hold hold, ReleaseDocumentHoldCommand command)
    {
        try
        {
            hold.Release(command.AuditContext.ActorId, command.ReasonCode, timeProvider.GetUtcNow());
            return Result.Ok(hold);
        }
        catch (ArgumentException invalid) when (string.Equals(invalid.ParamName, ReleasingActorParameter, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Releasing a hold requires an authenticated opaque actor identifier.", invalid);
        }
        catch (ArgumentException invalid)
        {
            return DocumentHoldErrors.InvalidField(invalid.ParamName ?? "hold").ConvertError<Hold>();
        }
    }
}
