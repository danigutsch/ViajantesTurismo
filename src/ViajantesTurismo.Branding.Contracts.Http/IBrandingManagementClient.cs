using PandaBear.Branding;

namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>Reads and conditionally replaces Branding settings through the management API.</summary>
public interface IBrandingManagementClient
{
    /// <summary>Reads the settings document and its entity tag.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The current document.</returns>
    Task<BrandingManagementDocument> GetSettings(CancellationToken ct);

    /// <summary>
    /// Replaces the settings when <paramref name="entityTag" /> still identifies the current revision. The response
    /// carries no validator, so read again before the next edit.
    /// </summary>
    /// <param name="request">The full replacement settings.</param>
    /// <param name="entityTag">The entity tag from the read the edit started from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The saved settings with their new revision.</returns>
    /// <exception cref="BrandingPreconditionFailedException">Another editor saved first.</exception>
    Task<BrandingManagementSettingsDto> SaveSettings(BrandingSettingsDto request, string entityTag, CancellationToken ct);
}
