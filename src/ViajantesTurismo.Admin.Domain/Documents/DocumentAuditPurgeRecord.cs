using JetBrains.Annotations;
using PandaBear.Domain;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Represents immutable, metadata-only evidence that an audit-retention purge ran.</summary>
public sealed class DocumentAuditPurgeRecord : IEntity<Guid>
{
    private DocumentAuditPurgeRecord(
        string actorId,
        string correlationId,
        int purgedCount,
        DateTime retentionCutoffUtc,
        DocumentAuditOutcome outcome,
        DateTime occurredAtUtc)
    {
        Id = Guid.CreateVersion7();
        ActorId = actorId;
        CorrelationId = correlationId;
        PurgedCount = purgedCount;
        RetentionCutoffUtc = retentionCutoffUtc;
        Outcome = outcome;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>DO NOT USE. Required by Entity Framework Core for materialization.</summary>
    [UsedImplicitly]
    private DocumentAuditPurgeRecord()
    {
    }

    /// <summary>Gets the opaque purge evidence identifier.</summary>
    public Guid Id
    {
        get; private init;
    }

    /// <summary>Gets the opaque identity that ran the purge.</summary>
    public string ActorId { get; private init; } = default!;

    /// <summary>Gets the server-generated correlation identifier.</summary>
    public string CorrelationId { get; private init; } = default!;

    /// <summary>Gets how many audit records the run removed.</summary>
    public int PurgedCount
    {
        get; private init;
    }

    /// <summary>Gets the instant at or before which audit records were eligible for removal.</summary>
    public DateTime RetentionCutoffUtc
    {
        get; private init;
    }

    /// <summary>Gets whether the purge run completed or failed.</summary>
    public DocumentAuditOutcome Outcome
    {
        get; private init;
    }

    /// <summary>Gets when the purge run finished in UTC.</summary>
    public DateTime OccurredAtUtc
    {
        get; private init;
    }

    /// <summary>Creates purge evidence from approved metadata only.</summary>
    public static Result<DocumentAuditPurgeRecord> Create(
        string actorId,
        string correlationId,
        int purgedCount,
        DateTime retentionCutoffUtc,
        DocumentAuditOutcome outcome,
        DateTime occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(actorId) || actorId.Length > DocumentAuditLimits.MaxActorIdLength)
        {
            return DocumentAuditErrors.InvalidActorId().ConvertError<DocumentAuditPurgeRecord>();
        }

        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > DocumentAuditLimits.MaxCorrelationIdLength)
        {
            return DocumentAuditErrors.InvalidCorrelationId().ConvertError<DocumentAuditPurgeRecord>();
        }

        if (occurredAtUtc.Kind != DateTimeKind.Utc || retentionCutoffUtc.Kind != DateTimeKind.Utc)
        {
            return DocumentAuditErrors.InvalidOccurredAtUtc().ConvertError<DocumentAuditPurgeRecord>();
        }

        if (!Enum.IsDefined(outcome) ||
            purgedCount < 0 ||
            (outcome == DocumentAuditOutcome.Rejected && purgedCount != 0))
        {
            return DocumentAuditErrors.InvalidEvidence().ConvertError<DocumentAuditPurgeRecord>();
        }

        return Result.Ok(new DocumentAuditPurgeRecord(
            actorId, correlationId, purgedCount, retentionCutoffUtc, outcome, occurredAtUtc));
    }
}
