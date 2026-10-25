namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>
/// Privileged Branding recovery operations. Ordinary management writes never rotate the epoch; run these from an
/// audited operator workflow after stopping Management writes and the Branding relay.
/// </summary>
public interface IBrandingSourceRecovery
{
    /// <summary>
    /// Republishes the current snapshot with the same epoch and revision under a new event identity, for an Admin
    /// projection that was lost or corrupted while the source lineage stayed intact.
    /// </summary>
    /// <param name="reason">Why recovery is needed.</param>
    /// <param name="incidentReference">The incident reference.</param>
    /// <param name="actor">The operator identity.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The enqueued reseed identity.</returns>
    Task<BrandingReseedPublication> RepublishCurrent(string reason, string incidentReference, string actor, CancellationToken ct);

    /// <summary>
    /// Rotates the source epoch after a restore that rewound the revision, appends the restored snapshot as the new
    /// epoch's revision 1, and enqueues it. Admin quarantines anything still pending from the old epoch.
    /// </summary>
    /// <param name="reason">Why recovery is needed.</param>
    /// <param name="incidentReference">The source backup or incident reference.</param>
    /// <param name="actor">The operator identity.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The enqueued reseed identity.</returns>
    Task<BrandingReseedPublication> RotateEpoch(string reason, string incidentReference, string actor, CancellationToken ct);
}
