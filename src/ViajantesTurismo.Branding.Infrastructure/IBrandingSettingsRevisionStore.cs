using PandaBear.Branding;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Reads and conditionally writes revisioned Branding settings for the management API.</summary>
public interface IBrandingSettingsRevisionStore
{
    /// <summary>Gets the current revisioned settings, or <see langword="null" /> before revisions start.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The current snapshot.</returns>
    Task<BrandingSettingsSnapshot?> GetCurrent(CancellationToken ct);

    /// <summary>
    /// Saves <paramref name="settings" /> only when <paramref name="precondition" /> holds for the current snapshot
    /// read in the write unit of work. A concurrent writer that commits first makes the save fail with a concurrency
    /// exception, never an overwrite.
    /// </summary>
    /// <param name="settings">The full replacement settings.</param>
    /// <param name="precondition">Evaluates the current snapshot, or <see langword="null" /> when none exists.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The saved snapshot, or <see langword="null" /> when the precondition is false and nothing was written.</returns>
    Task<BrandingSettingsSnapshot?> SaveIfMatch(
        BrandingSettings settings,
        Func<BrandingSettingsSnapshot?, bool> precondition,
        CancellationToken ct);

    /// <summary>
    /// Stores <paramref name="logo" /> (or clears the logo when it is <see langword="null" />) as a new settings revision
    /// when <paramref name="precondition" /> holds for the current snapshot read in the write unit of work.
    /// </summary>
    /// <param name="logo">The normalized replacement logo, or <see langword="null" /> to remove it.</param>
    /// <param name="precondition">Evaluates the current snapshot, or <see langword="null" /> when none exists.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The saved snapshot, or <see langword="null" /> when the precondition is false and nothing was written.</returns>
    Task<BrandingSettingsSnapshot?> ReplaceLogoIfMatch(
        BrandingLogo? logo,
        Func<BrandingSettingsSnapshot?, bool> precondition,
        CancellationToken ct);

    /// <summary>Gets a stored logo by its content address.</summary>
    /// <param name="sha256">The lowercase hexadecimal SHA-256 of the logo bytes.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The logo, or <see langword="null" /> when no logo has that address.</returns>
    Task<BrandingLogo?> GetLogo(string sha256, CancellationToken ct);
}
