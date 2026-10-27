using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>Persisted record of a branding event the projection has processed, used for idempotent redelivery.</summary>
internal sealed class BrandingInboxRecord
{
    private BrandingInboxRecord()
    {
    }

    /// <summary>Identifier of the processed integration event; primary key.</summary>
    public Guid EventId
    {
        get; private init;
    }

    /// <summary>Source epoch carried by the event.</summary>
    public Guid SourceEpoch
    {
        get; private init;
    }

    /// <summary>Source revision carried by the event.</summary>
    public long SourceRevision
    {
        get; private init;
    }

    /// <summary>How the event was handled.</summary>
    public BrandingInboxOutcome Outcome
    {
        get; private init;
    }

    /// <summary>Optional machine-readable reason code for the outcome; null when none applies.</summary>
    public string? ReasonCode
    {
        get; private init;
    }

    /// <summary>When the event was processed.</summary>
    public DateTimeOffset ProcessedAt
    {
        get; private init;
    }

    /// <summary>Creates a record from an application inbox entry.</summary>
    public static BrandingInboxRecord From(BrandingInboxEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new BrandingInboxRecord
        {
            EventId = entry.EventId,
            SourceEpoch = entry.SourceEpoch,
            SourceRevision = entry.SourceRevision,
            Outcome = entry.Outcome,
            ReasonCode = entry.ReasonCode,
            ProcessedAt = entry.ProcessedAt
        };
    }
}
