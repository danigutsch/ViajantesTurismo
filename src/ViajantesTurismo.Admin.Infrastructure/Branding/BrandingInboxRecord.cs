using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

internal sealed class BrandingInboxRecord
{
    private BrandingInboxRecord()
    {
    }

    public Guid EventId
    {
        get; private init;
    }

    public Guid SourceEpoch
    {
        get; private init;
    }

    public long SourceRevision
    {
        get; private init;
    }

    public BrandingInboxOutcome Outcome
    {
        get; private init;
    }

    public string? ReasonCode
    {
        get; private init;
    }

    public DateTimeOffset ProcessedAt
    {
        get; private init;
    }

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
