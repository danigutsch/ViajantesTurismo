using JetBrains.Annotations;
using PandaBear.Domain;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Represents immutable, counts-only evidence that a draft or idempotency retention purge ran.</summary>
public sealed class DocumentRetentionPurgeRecord : IEntity<Guid>
{
    private DocumentRetentionPurgeRecord(
        DocumentRetentionPurgeKind kind,
        int purgedCount,
        int heldCount,
        DocumentRetentionPurgeOutcome outcome,
        DateTime occurredAtUtc)
    {
        Id = Guid.CreateVersion7();
        Kind = kind;
        PurgedCount = purgedCount;
        HeldCount = heldCount;
        Outcome = outcome;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>DO NOT USE. Required by Entity Framework Core for materialization.</summary>
    [UsedImplicitly]
    private DocumentRetentionPurgeRecord()
    {
    }

    /// <summary>Gets the opaque purge evidence identifier.</summary>
    public Guid Id
    {
        get; private init;
    }

    /// <summary>Gets which retention purge ran.</summary>
    public DocumentRetentionPurgeKind Kind
    {
        get; private init;
    }

    /// <summary>Gets how many records the run removed.</summary>
    public int PurgedCount
    {
        get; private init;
    }

    /// <summary>Gets how many expired records the run skipped because a disposition hold restrained them.</summary>
    public int HeldCount
    {
        get; private init;
    }

    /// <summary>Gets whether the run completed or was refused.</summary>
    public DocumentRetentionPurgeOutcome Outcome
    {
        get; private init;
    }

    /// <summary>Gets when the purge run finished in UTC.</summary>
    public DateTime OccurredAtUtc
    {
        get; private init;
    }

    /// <summary>Creates purge evidence from counts only; a refused run must claim no removal and no held record.</summary>
    public static Result<DocumentRetentionPurgeRecord> Create(
        DocumentRetentionPurgeKind kind,
        int purgedCount,
        int heldCount,
        DocumentRetentionPurgeOutcome outcome,
        DateTime occurredAtUtc)
    {
        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            return DocumentRetentionPurgeErrors.InvalidOccurredAtUtc().ConvertError<DocumentRetentionPurgeRecord>();
        }

        if (!Enum.IsDefined(kind) ||
            !Enum.IsDefined(outcome) ||
            purgedCount < 0 ||
            heldCount < 0 ||
            (outcome != DocumentRetentionPurgeOutcome.Succeeded && (purgedCount != 0 || heldCount != 0)))
        {
            return DocumentRetentionPurgeErrors.InvalidEvidence().ConvertError<DocumentRetentionPurgeRecord>();
        }

        return Result.Ok(new DocumentRetentionPurgeRecord(kind, purgedCount, heldCount, outcome, occurredAtUtc));
    }
}
