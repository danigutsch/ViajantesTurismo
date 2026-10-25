using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

internal sealed class BrandingRecoveryAuditRecord
{
    private BrandingRecoveryAuditRecord()
    {
    }

    public Guid Id
    {
        get; private init;
    }

    public BrandingRecoveryAuditKind Kind
    {
        get; private init;
    }

    public Guid? OldEpoch
    {
        get; private init;
    }

    public long? OldRevision
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

    public static BrandingRecoveryAuditRecord From(BrandingRecoveryAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new BrandingRecoveryAuditRecord
        {
            Id = entry.Id,
            Kind = entry.Kind,
            OldEpoch = entry.OldEpoch,
            OldRevision = entry.OldRevision,
            NewEpoch = entry.NewEpoch,
            NewRevision = entry.NewRevision,
            EventId = entry.EventId,
            Reason = entry.Reason,
            IncidentReference = entry.IncidentReference,
            Actor = entry.Actor,
            RecordedAt = entry.RecordedAt
        };
    }
}
