using PandaBear.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Payments;

internal sealed class PaymentTestBrandingClient : IBrandingApiClient
{
    public Task<BrandingSettingsDto> GetPublicSettings(CancellationToken ct) => Task.FromResult(new BrandingSettingsDto
    {
        BrandName = "Viajantes",
        PrimaryColor = "#102030",
        AccentColor = "#405060",
        BackgroundColor = "#fdfdfd",
        TextColor = "#111111",
        HeadingFontFamily = "Montserrat",
        BodyFontFamily = "Inter"
    });
}
