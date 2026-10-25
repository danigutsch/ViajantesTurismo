using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

internal sealed class BrandingRecoveryRecord
{
    public static readonly Guid RecoveryId = Guid.Parse("8c1b0f4e-5d2a-4f6b-9a3e-2f7c1d9e4b60");

    private BrandingRecoveryRecord()
    {
    }

    public Guid Id
    {
        get; private init;
    }

    public BrandingProjectionStatus Status
    {
        get; private set;
    }

    public string? Reason
    {
        get; private set;
    }

    public DateTimeOffset? Since
    {
        get; private set;
    }

    public Guid? AuthorizedEventId
    {
        get; private set;
    }

    public Guid? AuthorizedEpoch
    {
        get; private set;
    }

    public long? AuthorizedRevision
    {
        get; private set;
    }

    public string? AuthorizationReason
    {
        get; private set;
    }

    public string? IncidentReference
    {
        get; private set;
    }

    public string? AuthorizedBy
    {
        get; private set;
    }

    public DateTimeOffset? AuthorizedAt
    {
        get; private set;
    }

    public static BrandingRecoveryRecord Create(BrandingProjectionRecovery recovery)
    {
        var record = new BrandingRecoveryRecord { Id = RecoveryId };
        record.ReplaceWith(recovery);
        return record;
    }

    public void ReplaceWith(BrandingProjectionRecovery recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        Status = recovery.Status;
        Reason = recovery.Reason;
        Since = recovery.Since;
        AuthorizedEventId = recovery.Authorization?.EventId;
        AuthorizedEpoch = recovery.Authorization?.TargetEpoch;
        AuthorizedRevision = recovery.Authorization?.TargetRevision;
        AuthorizationReason = recovery.Authorization?.Reason;
        IncidentReference = recovery.Authorization?.IncidentReference;
        AuthorizedBy = recovery.Authorization?.AuthorizedBy;
        AuthorizedAt = recovery.Authorization?.AuthorizedAt;
    }

    public BrandingProjectionRecovery ToRecovery()
    {
        var authorization = AuthorizedEventId is { } eventId
            && AuthorizedEpoch is { } epoch
            && AuthorizedRevision is { } revision
            && AuthorizedAt is { } authorizedAt
            ? new BrandingReseedAuthorization(
                eventId,
                epoch,
                revision,
                AuthorizationReason ?? string.Empty,
                IncidentReference ?? string.Empty,
                AuthorizedBy ?? string.Empty,
                authorizedAt)
            : null;
        return new BrandingProjectionRecovery(Status, Reason, Since, authorization);
    }
}
