using PandaBear.Branding;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>Reads and conditionally replaces Branding settings through the management API.</summary>
public interface IBrandingManagementClient
{
    /// <summary>Reads the settings document and its entity tag.</summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// The current document on success. A weak or missing entity tag is a malformed body because it cannot guard the
    /// next write.
    /// </returns>
    Task<HttpOutcome<BrandingManagementDocument>> GetSettings(CancellationToken ct);

    /// <summary>
    /// Replaces the settings when <paramref name="entityTag" /> still identifies the current revision. The response
    /// carries no validator, so read again before the next edit.
    /// </summary>
    /// <param name="request">The full replacement settings.</param>
    /// <param name="entityTag">The entity tag from the read the edit started from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// The saved settings with their new revision on success. A lost update is an unexpected-status outcome with
    /// status 412, a missing precondition has status 428, and a rejected <c>If-Match</c> syntax is a malformed body
    /// with status 400. The client never retries a 412 full-state mutation.
    /// </returns>
    Task<HttpOutcome<BrandingManagementSettingsDto>> SaveSettings(BrandingSettingsDto request, string entityTag, CancellationToken ct);

    /// <summary>
    /// Uploads a replacement logo when <paramref name="entityTag" /> still identifies the current revision. The server
    /// validates, scans and re-encodes the image; the response carries the generated logo URL.
    /// </summary>
    /// <param name="content">The raw image bytes.</param>
    /// <param name="contentType">The declared image media type: PNG, JPEG or WebP.</param>
    /// <param name="entityTag">The entity tag from the read the edit started from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// The saved settings on success. Invalid images are a validation outcome; a scanner outage has status 503;
    /// precondition failures follow <see cref="SaveSettings" />.
    /// </returns>
    Task<HttpOutcome<BrandingManagementSettingsDto>> UploadLogo(Stream content, string contentType, string entityTag, CancellationToken ct);

    /// <summary>Removes the logo when <paramref name="entityTag" /> still identifies the current revision.</summary>
    /// <param name="entityTag">The entity tag from the read the edit started from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The saved settings on success; precondition and availability failures follow <see cref="UploadLogo" />.</returns>
    Task<HttpOutcome<BrandingManagementSettingsDto>> RemoveLogo(string entityTag, CancellationToken ct);
}
