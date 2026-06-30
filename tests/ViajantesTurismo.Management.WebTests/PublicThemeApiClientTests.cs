using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

public sealed class PublicThemeApiClientTests
{
    [Fact]
    public async Task GetTheme_requests_management_public_theme_endpoint()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "primaryColor":"#112233",
                  "accentColor":"#445566",
                  "backgroundColor":"#FFFFFF",
                  "textColor":"#000000",
                  "headingFontFamily":"Inter",
                  "bodyFontFamily":"Verdana"
                }
                """);
        });
        var sut = new PublicThemeApiClient(httpClient);

        // Act
        var theme = await sut.GetTheme(Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/catalog/public-theme", requestPath);
        Assert.Equal("#112233", theme.PrimaryColor);
        Assert.Equal("Inter", theme.HeadingFontFamily);
    }

    [Fact]
    public async Task SaveTheme_sends_request_to_management_public_theme_endpoint()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "primaryColor":"#112233",
                  "accentColor":"#445566",
                  "backgroundColor":"#FFFFFF",
                  "textColor":"#000000",
                  "headingFontFamily":"Inter",
                  "bodyFontFamily":"Verdana"
                }
                """);
        });
        var sut = new PublicThemeApiClient(httpClient);
        var request = new PublicThemeSettingsDto
        {
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        };

        // Act
        var theme = await sut.SaveTheme(request, Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/catalog/public-theme", requestPath);
        Assert.Equal("Verdana", theme.BodyFontFamily);
    }
}
