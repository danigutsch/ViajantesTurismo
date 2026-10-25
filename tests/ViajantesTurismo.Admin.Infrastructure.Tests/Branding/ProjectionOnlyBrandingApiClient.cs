using PandaBear.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Branding;

internal sealed class ProjectionOnlyBrandingApiClient : IBrandingApiClient
{
    public Task<BrandingSettingsDto> GetPublicSettings(CancellationToken ct) =>
        throw new InvalidOperationException("Projection-sourced capture must not call Branding over HTTP.");
}
