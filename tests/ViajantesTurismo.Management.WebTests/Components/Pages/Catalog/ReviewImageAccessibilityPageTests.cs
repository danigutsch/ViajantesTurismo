using ReviewImageAccessibility = ViajantesTurismo.Management.Web.Components.Pages.Catalog.ReviewImageAccessibility;
using Microsoft.AspNetCore.Routing;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

public sealed class ReviewImageAccessibilityPageTests : BunitContext
{
    private readonly FakeCatalogToursApiClient catalogApi = new();

    public ReviewImageAccessibilityPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICatalogToursApiClient>(catalogApi);
        Services.AddSingleton<LinkGenerator>(new TestLinkGenerator((endpointName, values) =>
        {
            if (!string.Equals(endpointName, ManagementWebEndpoints.MediaPreviewByRenditionEndpointName, StringComparison.Ordinal))
            {
                return null;
            }

            return $"/catalog/media/images/{values["id"]}/preview/{values["width"]}/{values["format"]}";
        }));
    }

    [Fact]
    public void Approves_the_generated_alt_text_when_the_draft_is_not_decorative()
    {
        // Arrange
        var image = new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            ResponsiveVariants =
            [
                new CatalogMediaImageVariantDto
                {
                    Width = 640,
                    Height = 427,
                    ContentType = "image/jpeg",
                    FileSizeBytes = 640
                }
            ],
            AltText = string.Empty,
            IsDecorative = true,
            RequiresHumanReview = true,
            IsAiGenerated = false
        };
        catalogApi.Images = [image];
        catalogApi.Draft = image with
        {
            AltText = "A cyclist riding through a mountain pass.",
            IsDecorative = false,
            IsAiGenerated = true
        };

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.Markup.Contains("Editor-provided", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        var previewPath = cut.Find("img").GetAttribute("src");
        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForState(() => cut.Markup.Contains("A cyclist riding through a mountain pass.", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        cut.Find("button.btn-primary").Click();
        cut.WaitForState(() => catalogApi.LastAccessibilityReviewRequest is not null, TimeSpan.FromSeconds(2));

        // Assert
        var request = catalogApi.LastAccessibilityReviewRequest.ShouldNotBeNull();
        previewPath.ShouldBe($"/catalog/media/images/{image.Id}/preview/640/jpg");
        request.IsDecorative.ShouldBeFalse();
        request.AltText.ShouldBe("A cyclist riding through a mountain pass.");
    }
}
