using PandaBear.DispositionHold;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Places a disposition hold on a document lineage, serialized against any purge of the lineage.</summary>
public sealed class PlaceDocumentHoldCommandHandler(
    IDocumentHoldQuery holdQuery,
    IDispositionHoldStore holdStore,
    IUnitOfWork unitOfWork,
    IDocumentDispositionTransaction transaction,
    TimeProvider timeProvider)
{
    private const string PlacingActorParameter = "placedBy";

    /// <summary>Places the hold and returns it, or a not-found or validation failure that stores nothing.</summary>
    public async Task<Result<Hold>> Handle(PlaceDocumentHoldCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lineageId = await holdQuery.FindLineageId(command.DocumentId, ct);
        if (lineageId is null)
        {
            return DocumentErrors.DocumentNotFound().ConvertError<Hold>();
        }

        var placed = TryPlace(command, lineageId.Value);
        return placed.IsFailure
            ? placed
            : await transaction.Execute([], () => Store(command.DocumentId, placed.Value, ct), ct);
    }

    private async Task<Result<Hold>> Store(Guid documentId, Hold hold, CancellationToken ct)
    {
        await holdStore.Add(hold, ct);
        if (await holdQuery.FindLineageId(documentId, ct) is null)
        {
            return DocumentErrors.DocumentNotFound().ConvertError<Hold>();
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok(hold);
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
                command.ActorId,
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
