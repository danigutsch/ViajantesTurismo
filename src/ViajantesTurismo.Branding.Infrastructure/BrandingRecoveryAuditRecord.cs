namespace ViajantesTurismo.Branding.Infrastructure;

internal sealed class BrandingRecoveryAuditRecord
{
    private BrandingRecoveryAuditRecord()
    {
    }

    public Guid Id
    {
        get; private init;
    }

    public string Kind { get; private init; } = string.Empty;

    public Guid OldEpoch
    {
        get; private init;
    }

    public long OldRevision
    {
        get; private init;
    }

    public Guid NewEpoch
    {
        get; private init;
    }

    public long NewRevision
    {
        get; private init;
    }

    public Guid EventId
    {
        get; private init;
    }

    public string Reason { get; private init; } = string.Empty;

    public string IncidentReference { get; private init; } = string.Empty;

    public string Actor { get; private init; } = string.Empty;

    public DateTimeOffset RecordedAt
    {
        get; private init;
    }

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
