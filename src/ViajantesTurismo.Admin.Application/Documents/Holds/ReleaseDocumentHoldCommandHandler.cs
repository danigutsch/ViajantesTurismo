using PandaBear.DispositionHold;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Releases a disposition hold once, recording the releasing actor and reason code.</summary>
public sealed class ReleaseDocumentHoldCommandHandler(
    IDocumentHoldQuery holdQuery,
    IDispositionHoldStore holdStore,
    IUnitOfWork unitOfWork,
    IDocumentDispositionTransaction transaction,
    TimeProvider timeProvider)
{
    private const string ReleasingActorParameter = "releasedBy";
    private const string ReleaseLockPrefix = "admin:document-hold-release:";

    /// <summary>Releases the hold and returns it; a second release is a conflict and a foreign hold is not found.</summary>
    public async Task<Result<Hold>> Handle(ReleaseDocumentHoldCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lineageId = await holdQuery.FindLineageId(command.DocumentId, ct);
        if (lineageId is null)
        {
            return DocumentErrors.DocumentNotFound().ConvertError<Hold>();
        }

        var lockName = string.Concat(ReleaseLockPrefix, command.HoldId.ToString("N"));
        return await transaction.Execute([lockName], () => Release(command, DocumentHoldScopes.ForLineage(lineageId.Value), ct), ct);
    }

    private async Task<Result<Hold>> Release(ReleaseDocumentHoldCommand command, DispositionScope scope, CancellationToken ct)
    {
        var hold = await holdStore.Find(command.HoldId, ct);
        if (hold is null || hold.Scope != scope)
        {
            return DocumentHoldErrors.HoldNotFound().ConvertError<Hold>();
        }

        if (hold.IsReleased)
        {
            return DocumentHoldErrors.HoldAlreadyReleased().ConvertError<Hold>();
        }

        var released = TryRelease(hold, command);
        if (released.IsFailure)
        {
            return released;
        }

        await unitOfWork.SaveEntities(ct);
        return released;
    }

    private Result<Hold> TryRelease(Hold hold, ReleaseDocumentHoldCommand command)
    {
        try
        {
            hold.Release(command.ActorId, command.ReasonCode, timeProvider.GetUtcNow());
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
