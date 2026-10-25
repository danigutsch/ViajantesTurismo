namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>An append-only record of one completed projection recovery.</summary>
/// <param name="Id">The audit identifier.</param>
/// <param name="Kind">The recovery transition.</param>
/// <param name="OldEpoch">The epoch before the reseed, or <see langword="null" /> when the projection was lost.</param>
/// <param name="OldRevision">The revision before the reseed, or <see langword="null" /> when the projection was lost.</param>
/// <param name="NewEpoch">The epoch after the reseed.</param>
/// <param name="NewRevision">The revision after the reseed.</param>
/// <param name="EventId">The applied reseed envelope identity.</param>
/// <param name="Reason">The recovery reason.</param>
/// <param name="IncidentReference">The source backup or incident reference.</param>
/// <param name="Actor">The operator who authorized the reseed.</param>
/// <param name="RecordedAt">When the reseed committed.</param>
public sealed record BrandingRecoveryAuditEntry(
    Guid Id,
    BrandingRecoveryAuditKind Kind,
    Guid? OldEpoch,
    long? OldRevision,
    Guid NewEpoch,
    long NewRevision,
    Guid EventId,
    string Reason,
    string IncidentReference,
    string Actor,
    DateTimeOffset RecordedAt);
