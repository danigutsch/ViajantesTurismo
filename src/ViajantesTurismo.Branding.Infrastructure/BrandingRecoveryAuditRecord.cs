namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Audit trail row for a Branding source recovery action such as a reseed or epoch rotation.</summary>
internal sealed class BrandingRecoveryAuditRecord
{
    private BrandingRecoveryAuditRecord()
    {
    }

    /// <summary>Unique identifier of the audit record.</summary>
    public Guid Id
    {
        get; private init;
    }

    /// <summary>Kind of recovery action, for example same-epoch-reseed.</summary>
    public string Kind { get; private init; } = string.Empty;

    /// <summary>Source epoch before the recovery.</summary>
    public Guid OldEpoch
    {
        get; private init;
    }

    /// <summary>Source revision before the recovery.</summary>
    public long OldRevision
    {
        get; private init;
    }

    /// <summary>Source epoch published by the recovery.</summary>
    public Guid NewEpoch
    {
        get; private init;
    }

    /// <summary>Source revision published by the recovery.</summary>
    public long NewRevision
    {
        get; private init;
    }

    /// <summary>Identifier of the integration event published by the recovery.</summary>
    public Guid EventId
    {
        get; private init;
    }

    /// <summary>Operator-supplied reason for the recovery.</summary>
    public string Reason { get; private init; } = string.Empty;

    /// <summary>Reference to the incident that required the recovery.</summary>
    public string IncidentReference { get; private init; } = string.Empty;

    /// <summary>Operator who performed the recovery.</summary>
    public string Actor { get; private init; } = string.Empty;

    /// <summary>When the recovery was recorded.</summary>
    public DateTimeOffset RecordedAt
    {
        get; private init;
    }

    /// <summary>Creates an audit record for a recovery that published the given reseed.</summary>
    public static BrandingRecoveryAuditRecord Create(
        string kind,
        Guid oldEpoch,
        long oldRevision,
        BrandingReseedPublication publication,
        string reason,
        string incidentReference,
        string actor,
        DateTimeOffset recordedAt) => new()
        {
            Id = Guid.CreateVersion7(),
            Kind = kind,
            OldEpoch = oldEpoch,
            OldRevision = oldRevision,
            NewEpoch = publication.SourceEpoch,
            NewRevision = publication.SourceRevision,
            EventId = publication.EventId,
            Reason = reason,
            IncidentReference = incidentReference,
            Actor = actor,
            RecordedAt = recordedAt
        };
}
