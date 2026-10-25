namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>An operator's one-shot authorization for the exact reseed event that ends a recovery.</summary>
/// <param name="EventId">The envelope identity of the reseed event.</param>
/// <param name="TargetEpoch">The source epoch the reseed carries.</param>
/// <param name="TargetRevision">The source revision the reseed carries.</param>
/// <param name="Reason">Why the recovery is needed.</param>
/// <param name="IncidentReference">The source backup or incident reference.</param>
/// <param name="AuthorizedBy">The operator identity.</param>
/// <param name="AuthorizedAt">When the authorization was recorded.</param>
public sealed record BrandingReseedAuthorization(
    Guid EventId,
    Guid TargetEpoch,
    long TargetRevision,
    string Reason,
    string IncidentReference,
    string AuthorizedBy,
    DateTimeOffset AuthorizedAt)
{
    /// <summary>Gets whether the delivered event is exactly the authorized reseed.</summary>
    /// <param name="eventId">The delivered envelope identity.</param>
    /// <param name="sourceEpoch">The delivered source epoch.</param>
    /// <param name="sourceRevision">The delivered source revision.</param>
    /// <returns><see langword="true" /> when every identity matches.</returns>
    public bool Matches(Guid eventId, Guid sourceEpoch, long sourceRevision) =>
        eventId == EventId && sourceEpoch == TargetEpoch && sourceRevision == TargetRevision;
}
