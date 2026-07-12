using SharedKernel.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal sealed class FakeBrandingApiClient(BrandingSettingsDto settings) : IBrandingApiClient
{
    public Task<BrandingSettingsDto> GetPublicSettings(CancellationToken ct) => Task.FromResult(settings);

    public Task<BrandingSettingsDto> GetSettings(CancellationToken ct) => Task.FromResult(settings);

    public Task<BrandingSettingsDto> SaveSettings(BrandingSettingsDto request, CancellationToken ct) => Task.FromResult(request);
}
