namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>The reseed event a source recovery enqueued; the operator authorizes exactly this identity in Admin.</summary>
/// <param name="EventId">The reseed envelope identity.</param>
/// <param name="SourceEpoch">The epoch the reseed carries.</param>
/// <param name="SourceRevision">The revision the reseed carries.</param>
public sealed record BrandingReseedPublication(Guid EventId, Guid SourceEpoch, long SourceRevision);
