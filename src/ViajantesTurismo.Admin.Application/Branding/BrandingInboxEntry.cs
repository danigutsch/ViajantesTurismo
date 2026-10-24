namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>A processed Branding event, recorded with the projection change in one transaction.</summary>
/// <param name="EventId">The delivered event identifier.</param>
/// <param name="SourceEpoch">The event's source epoch.</param>
/// <param name="SourceRevision">The event's source revision.</param>
/// <param name="Outcome">How the event was settled.</param>
/// <param name="ReasonCode">A sanitized reason code for a rejected event; never payload values.</param>
/// <param name="ProcessedAt">When the event was settled.</param>
public sealed record BrandingInboxEntry(
    Guid EventId,
    Guid SourceEpoch,
    long SourceRevision,
    BrandingInboxOutcome Outcome,
    string? ReasonCode,
    DateTimeOffset ProcessedAt);
