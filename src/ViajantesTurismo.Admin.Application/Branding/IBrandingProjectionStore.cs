namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>Persists the Admin Branding projection and its inbox in one unit of work.</summary>
public interface IBrandingProjectionStore
{
    /// <summary>Gets the applied projection, or <see langword="null" /> before the first revision.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The applied projection state.</returns>
    Task<BrandingProjectionState?> GetCurrent(CancellationToken ct);

    /// <summary>Gets whether the inbox already settled an event.</summary>
    /// <param name="eventId">The delivered event identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true" /> when the event was already settled.</returns>
    Task<bool> WasProcessed(Guid eventId, CancellationToken ct);

    /// <summary>Stages a projection replacement.</summary>
    /// <param name="state">The new projection state.</param>
    void Replace(BrandingProjectionState state);

    /// <summary>Stages an inbox entry.</summary>
    /// <param name="entry">The settled event.</param>
    void Record(BrandingInboxEntry entry);

    /// <summary>Commits the staged projection and inbox changes together.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that completes when the changes commit.</returns>
    Task Save(CancellationToken ct);
}
