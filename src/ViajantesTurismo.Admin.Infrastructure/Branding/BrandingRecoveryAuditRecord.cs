using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>Append-only audit row for a branding projection recovery action.</summary>
internal sealed class BrandingRecoveryAuditRecord
{
    private BrandingRecoveryAuditRecord()
    {
    }

    /// <summary>Audit entry identifier; primary key.</summary>
    public Guid Id
    {
        get; private init;
    }

    /// <summary>Kind of recovery action recorded.</summary>
    public BrandingRecoveryAuditKind Kind
    {
        get; private init;
    }

    /// <summary>Epoch before the action; null when there was no prior state.</summary>
    public Guid? OldEpoch
    {
        get; private init;
    }

    /// <summary>Revision before the action; null when there was no prior state.</summary>
    public long? OldRevision
    {
        get; private init;
    }

    /// <summary>Epoch after the action.</summary>
    public Guid NewEpoch
    {
        get; private init;
    }

    /// <summary>Revision after the action.</summary>
    public long NewRevision
    {
        get; private init;
    }

    /// <summary>Integration event that triggered the action.</summary>
    public Guid EventId
    {
        get; private init;
    }

    /// <summary>Operator-supplied reason.</summary>
    public string Reason { get; private init; } = string.Empty;

    /// <summary>Incident reference justifying the action.</summary>
    public string IncidentReference { get; private init; } = string.Empty;

    /// <summary>Identity of the actor who performed or authorized the action.</summary>
    public string Actor { get; private init; } = string.Empty;

    /// <summary>When the action was recorded.</summary>
    public DateTimeOffset RecordedAt
    {
        get; private init;
    }

    /// <summary>Creates a record from an application audit entry.</summary>
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
