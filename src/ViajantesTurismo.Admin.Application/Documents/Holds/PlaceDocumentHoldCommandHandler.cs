using PandaBear.DispositionHold;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>
/// Places a disposition hold on a document lineage, serialized against any purge of the lineage, and records the
/// placement or the rejection in the document audit trail.
/// </summary>
public sealed class PlaceDocumentHoldCommandHandler(
    IDocumentHoldQuery holdQuery,
    IDispositionHoldStore holdStore,
    IDocumentDispositionTransaction transaction,
    TimeProvider timeProvider,
    DocumentAuditWriter auditWriter)
{
    private const string PlacingActorParameter = "placedBy";

    /// <summary>Places the hold and returns it, or a not-found or validation failure that stores nothing.</summary>
    public async Task<Result<Hold>> Handle(PlaceDocumentHoldCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.AuditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired().ConvertError<Hold>();
        }

        var lineageId = await holdQuery.FindLineageId(command.DocumentId, ct);
        if (lineageId is null)
        {
            return await Reject(DocumentErrors.DocumentNotFound().ConvertError<Hold>(), command, DocumentAuditReasonCode.DocumentNotFound, ct);
        }

        var placed = TryPlace(command, lineageId.Value);
        return placed.IsFailure
            ? await Reject(placed, command, DocumentAuditReasonCode.ValidationRejected, ct)
            : await transaction.Execute([], () => Store(command, placed.Value, ct), ct);
    }

    private async Task<Result<Hold>> Reject(
        Result<Hold> rejection,
        PlaceDocumentHoldCommand command,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct)
    {
        var audit = await auditWriter.AddHold(
            command.AuditContext,
            DocumentAuditOperation.PlaceHold,
            command.DocumentId,
            null,
            command.Basis.ToString(),
            DocumentAuditOutcome.Rejected,
            reasonCode,
            ct);
        return audit.IsFailure ? audit.ConvertError<Hold>() : rejection;
    }

    private async Task<Result<Hold>> Store(PlaceDocumentHoldCommand command, Hold hold, CancellationToken ct)
    {
        await holdStore.Add(hold, ct);
        if (await holdQuery.FindLineageId(command.DocumentId, ct) is null)
        {
            return DocumentErrors.DocumentNotFound().ConvertError<Hold>();
        }

        var audit = await auditWriter.AddHold(
            command.AuditContext,
            DocumentAuditOperation.PlaceHold,
            command.DocumentId,
            hold.Id,
            hold.Basis.ToString(),
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualHoldPlacement,
            ct);
        return audit.IsFailure ? audit.ConvertError<Hold>() : Result.Ok(hold);
    }

    private Result<Hold> TryPlace(PlaceDocumentHoldCommand command, Guid lineageId)
    {
        var now = timeProvider.GetUtcNow();
        try
        {
            return Result.Ok(Hold.Place(
                DocumentHoldScopes.ForLineage(lineageId),
                command.Basis,
                command.PurposeCode,
                command.AuthorityReference,
                command.AuditContext.ActorId,
                now,
                now,
                command.ReviewDueAt,
                command.ExpiresAt));
        }
        catch (ArgumentException invalid) when (string.Equals(invalid.ParamName, PlacingActorParameter, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Placing a hold requires an authenticated opaque actor identifier.", invalid);
        }
        catch (ArgumentException invalid)
        {
            return DocumentHoldErrors.InvalidField(invalid.ParamName ?? "hold").ConvertError<Hold>();
        }
    }
}
