namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>An operator request to authorize the reseed event that ends a projection recovery.</summary>
/// <param name="ExpectedEpoch">The epoch the operator believes Admin holds, or <see langword="null" /> when lost.</param>
/// <param name="ExpectedRevision">The revision the operator believes Admin holds, or <see langword="null" /> when lost.</param>
/// <param name="EventId">The envelope identity Branding published for the reseed.</param>
/// <param name="TargetEpoch">The epoch the reseed carries.</param>
/// <param name="TargetRevision">The revision the reseed carries.</param>
/// <param name="Reason">Why recovery is needed.</param>
/// <param name="IncidentReference">The source backup or incident reference.</param>
/// <param name="Actor">The operator identity.</param>
public sealed record BrandingReseedRequest(
    Guid? ExpectedEpoch,
    long? ExpectedRevision,
    Guid EventId,
    Guid TargetEpoch,
    long TargetRevision,
    string Reason,
    string IncidentReference,
    string Actor);
