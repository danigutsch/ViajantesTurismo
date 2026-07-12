using SharedKernel.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class FakeBrandingApiClient(BrandingSettingsDto settings) : IBrandingApiClient
{
    public Task<BrandingSettingsDto> GetPublicSettings(CancellationToken ct) => Task.FromResult(settings);
}
