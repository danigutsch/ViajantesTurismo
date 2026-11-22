using PandaBear.DispositionHold;
using PandaBear.Mediator;
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
    TimeProvider timeProvider,
    DocumentAuditWriter auditWriter) : ICommandHandler<ReleaseDocumentHoldCommand, Result<Hold>>
{
    private const string ReleasingActorParameter = "releasedBy";

    /// <summary>Releases the hold and returns it; a second release is a conflict and a foreign hold is not found.</summary>
    public async ValueTask<Result<Hold>> Handle(ReleaseDocumentHoldCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.ClearRejectionAudit();
        if (command.AuditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired().ConvertError<Hold>();
        }

        if (command.HoldId == Guid.Empty)
        {
            return DocumentAuditErrors.InvalidResourceId().ConvertError<Hold>();
        }

        var lineageId = await holdQuery.FindLineageId(command.DocumentId, ct);
        if (lineageId is null)
        {
            return DeferRejection(DocumentErrors.DocumentNotFound().ConvertError<Hold>(), command, null, DocumentAuditReasonCode.DocumentNotFound);
        }

        var hold = await holdStore.Find(command.HoldId, ct);
        if (hold is null || hold.Scope != DocumentHoldScopes.ForLineage(lineageId.Value))
        {
            return DeferRejection(DocumentHoldErrors.HoldNotFound().ConvertError<Hold>(), command, lineageId, DocumentAuditReasonCode.HoldNotFound);
        }

        if (hold.IsReleased)
        {
            return DeferRejection(DocumentHoldErrors.HoldAlreadyReleased().ConvertError<Hold>(), command, lineageId, DocumentAuditReasonCode.StateConflict, hold);
        }

        var released = TryRelease(hold, command);
        if (released.IsFailure)
        {
            return DeferRejection(released, command, lineageId, DocumentAuditReasonCode.ValidationRejected, hold);
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

    private static Result<Hold> DeferRejection(
        Result<Hold> rejection,
        ReleaseDocumentHoldCommand command,
        Guid? lineageId,
        DocumentAuditReasonCode reasonCode,
        Hold? hold = null)
    {
        command.DeferRejectionAudit(new DocumentRejectionAudit(
            reasonCode,
            DocumentId: lineageId is null ? null : command.DocumentId,
            DocumentLineageId: lineageId,
            HoldBasis: hold?.Basis.ToString(),
            HoldId: command.HoldId));
        return rejection;
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
