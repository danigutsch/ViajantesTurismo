using ReviewImageAccessibility = ViajantesTurismo.Management.Web.Components.Pages.Catalog.ReviewImageAccessibility;
using Microsoft.AspNetCore.Routing;
using SharedKernel.HttpClients;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
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

    [Fact]
    public void Keeps_success_confirmation_visible_after_approving_the_last_image()
    {
        // Arrange
        var image = new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Cyclists crossing a mountain pass.",
            RequiresHumanReview = true,
            ResponsiveVariants =
            [
                new CatalogMediaImageVariantDto
                {
                    Width = 640,
                    Height = 427,
                    ContentType = "image/jpeg",
                    FileSizeBytes = 640
                }
            ]
        };
        catalogApi.Images = [image];
        catalogApi.AccessibilityReviewResult = image with { RequiresHumanReview = false };

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.FindAll("button.btn-primary").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find("button.btn-primary").Click();
        cut.WaitForState(
            () => cut.Markup.Contains("Accessibility text approved for publication.", StringComparison.Ordinal),
            TimeSpan.FromSeconds(2));

        // Assert
        cut.FindAll("[role='status']").Select(element => element.TextContent)
            .ShouldContain(text => text.Contains("Accessibility text approved for publication.", StringComparison.Ordinal));
        cut.Markup.ShouldContain("No images require accessibility review.", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_no_pending_review_state_when_all_images_are_already_reviewed()
    {
        // Arrange
        catalogApi.Images =
        [
            new CatalogMediaImageDto
            {
                Id = Guid.CreateVersion7(),
                AltText = "Reviewed image",
                RequiresHumanReview = false,
                ResponsiveVariants =
                [
                    new CatalogMediaImageVariantDto
                    {
                        Width = 640,
                        Height = 427,
                        ContentType = "image/jpeg",
                        FileSizeBytes = 640
                    }
                ]
            }
        ];

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(
            () => cut.Markup.Contains("No images require accessibility review.", StringComparison.Ordinal),
            TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("[role='status']").TextContent.ShouldBe("No images require accessibility review.");
        cut.FindAll("button.btn-primary").ShouldBeEmpty();
    }

    [Fact]
    public void Approves_a_decorative_image_without_sending_alt_text()
    {
        // Arrange
        var image = new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Existing description",
            IsDecorative = true,
            RequiresHumanReview = true,
            ResponsiveVariants =
            [
                new CatalogMediaImageVariantDto
                {
                    Width = 640,
                    Height = 427,
                    ContentType = "image/jpeg",
                    FileSizeBytes = 640
                }
            ]
        };
        catalogApi.Images = [image];

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.FindAll("button.btn-primary").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find("button.btn-primary").Click();
        cut.WaitForState(() => catalogApi.LastAccessibilityReviewRequest is not null, TimeSpan.FromSeconds(2));

        // Assert
        var request = catalogApi.LastAccessibilityReviewRequest.ShouldNotBeNull();
        request.IsDecorative.ShouldBeTrue();
        request.AltText.ShouldBeNull();
    }

    [Fact]
    public void Shows_an_error_when_loading_the_next_image_fails_after_approval()
    {
        // Arrange
        var image = new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Cyclists crossing a mountain pass.",
            RequiresHumanReview = true,
            ResponsiveVariants =
            [
                new CatalogMediaImageVariantDto
                {
                    Width = 640,
                    Height = 427,
                    ContentType = "image/jpeg",
                    FileSizeBytes = 640
                }
            ]
        };
        catalogApi.Images = [image];
        catalogApi.ThrowOnSubsequentGetTourImages = true;

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.FindAll("button.btn-primary").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find("button.btn-primary").Click();
        cut.WaitForState(
            () => cut.Markup.Contains("The next image could not be loaded.", StringComparison.Ordinal),
            TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldBe("The next image could not be loaded. Refresh the page and try again.");
    }

    [Fact]
    public void Shows_an_error_when_images_cannot_be_loaded()
    {
        // Arrange
        catalogApi.ThrowOnGetTourImages = true;

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(
            () => cut.Markup.Contains("Images could not be loaded.", StringComparison.Ordinal),
            TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldBe("Images could not be loaded. Try again later.");
        cut.FindAll("[role='status']").ShouldBeEmpty();
    }

    [Fact]
    public void Shows_not_found_when_the_selected_image_disappears_during_approval()
    {
        // Arrange
        catalogApi.Images =
        [
            new CatalogMediaImageDto
            {
                Id = Guid.CreateVersion7(),
                AltText = "Cyclists crossing a mountain pass.",
                ResponsiveVariants = [],
                RequiresHumanReview = true
            }
        ];
        catalogApi.ReturnNullOnAccessibilityReview = true;

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.FindAll("button.btn-primary").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find("button.btn-primary").Click();
        cut.WaitForState(() => cut.Markup.Contains("Image was not found.", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldBe("Image was not found.");
    }

    [Fact]
    public void Shows_server_validation_errors_when_approval_is_rejected()
    {
        // Arrange
        catalogApi.Images =
        [
            new CatalogMediaImageDto
            {
                Id = Guid.CreateVersion7(),
                AltText = "Cyclists crossing a mountain pass.",
                ResponsiveVariants = [],
                RequiresHumanReview = true
            }
        ];
        catalogApi.ValidationException = new ContractValidationException(
            "Validation failed.",
            new Dictionary<string, string[]>
            {
                ["altText"] = ["Image description is required."]
            });

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.FindAll("button.btn-primary").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find("button.btn-primary").Click();
        cut.WaitForState(
            () => cut.Markup.Contains("Image description is required.", StringComparison.Ordinal),
            TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldBe("Image description is required.");
    }
}
