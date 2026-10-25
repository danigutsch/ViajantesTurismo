using PandaBear.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class UnusedBrandingApiClient : IBrandingApiClient
{
    public Task<BrandingSettingsDto> GetPublicSettings(CancellationToken ct) =>
        throw new InvalidOperationException("Projection-sourced capture must not call Branding over HTTP.");
}
