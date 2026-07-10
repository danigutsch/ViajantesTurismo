using TestTraits = ViajantesTurismo.Branding.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class BrandingApiSecurityBaselineTests
{
    [Fact]
    public async Task Public_branding_reads_return_too_many_requests_after_policy_limit()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        for (var requestNumber = 0; requestNumber < 60; requestNumber++)
        {
            using var allowedResponse = await client.GetAsync(new Uri("/api/v1/public/branding", UriKind.Relative), TestContext.Current.CancellationToken);
            allowedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var limitedResponse = await client.GetAsync(new Uri("/api/v1/public/branding", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Branding_management_mutations_return_too_many_requests_after_policy_limit()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new BrandingSettingsDto
        {
            BrandName = "Camino Riders",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana",
            LogoUri = null
        };

        // Act
        for (var requestNumber = 0; requestNumber < 20; requestNumber++)
        {
            using var allowedResponse = await client.PutAsJsonAsync(new Uri("/api/v1/branding/settings", UriKind.Relative), request, TestContext.Current.CancellationToken);
            allowedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var limitedResponse = await client.PutAsJsonAsync(new Uri("/api/v1/branding/settings", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
