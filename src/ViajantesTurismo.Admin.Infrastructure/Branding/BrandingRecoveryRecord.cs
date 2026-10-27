using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>Persisted single-row recovery fence for the branding projection, including any reseed authorization.</summary>
internal sealed class BrandingRecoveryRecord
{
    /// <summary>Fixed key of the single recovery row.</summary>
    public static readonly Guid RecoveryId = Guid.Parse("8c1b0f4e-5d2a-4f6b-9a3e-2f7c1d9e4b60");

    private BrandingRecoveryRecord()
    {
    }

    /// <summary>Row key; always <see cref="RecoveryId" />.</summary>
    public Guid Id
    {
        get; private init;
    }

    /// <summary>Current projection status.</summary>
    public BrandingProjectionStatus Status
    {
        get; private set;
    }

    /// <summary>Why the projection is not ready; null when ready.</summary>
    public string? Reason
    {
        get; private set;
    }

    /// <summary>When the current status began; null when ready.</summary>
    public DateTimeOffset? Since
    {
        get; private set;
    }

    /// <summary>Event id of the reseed authorization; null when none is authorized.</summary>
    public Guid? AuthorizedEventId
    {
        get; private set;
    }

    /// <summary>Source epoch the reseed is authorized for.</summary>
    public Guid? AuthorizedEpoch
    {
        get; private set;
    }

    /// <summary>Source revision the reseed is authorized for.</summary>
    public long? AuthorizedRevision
    {
        get; private set;
    }

    /// <summary>Reason given for the reseed authorization.</summary>
    public string? AuthorizationReason
    {
        get; private set;
    }

    /// <summary>Incident reference for the reseed authorization.</summary>
    public string? IncidentReference
    {
        get; private set;
    }

    /// <summary>Who authorized the reseed.</summary>
    public string? AuthorizedBy
    {
        get; private set;
    }

    /// <summary>When the reseed was authorized.</summary>
    public DateTimeOffset? AuthorizedAt
    {
        get; private set;
    }

    /// <summary>Creates the recovery row from a recovery value.</summary>
    public static BrandingRecoveryRecord Create(BrandingProjectionRecovery recovery)
    {
        var record = new BrandingRecoveryRecord { Id = RecoveryId };
        record.ReplaceWith(recovery);
        return record;
    }

    /// <summary>Overwrites status and authorization columns from a recovery value.</summary>
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

    /// <summary>Converts the row back to an application recovery value, with no authorization unless all required columns are set.</summary>
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
