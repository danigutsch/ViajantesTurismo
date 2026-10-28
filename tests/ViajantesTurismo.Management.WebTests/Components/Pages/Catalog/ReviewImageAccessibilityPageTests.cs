using PandaBear.Testing;
using System.Net;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing;
using PandaBear.HttpClients;
using ViajantesTurismo.Management.Web;
using ReviewImageAccessibility = ViajantesTurismo.Management.Web.Components.Pages.Catalog.ReviewImageAccessibility;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class ReviewImageAccessibilityPageTests : BunitContext
{
    private readonly FakeCatalogToursApiClient catalogApi = new();

    public ReviewImageAccessibilityPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICatalogToursApiClient>(catalogApi);
        Services.AddRouting();
        Services.AddSingleton<EndpointDataSource>(new DefaultEndpointDataSource(
            new RouteEndpoint(_ => Task.CompletedTask,
                RoutePatternFactory.Parse("/catalog/media/images/{id}/preview/{width}/{format}"), 0,
                new EndpointMetadataCollection(new EndpointNameMetadata(ManagementWebEndpoints.MediaPreviewByRenditionEndpointName)), "media")));
    }

    [Fact]
    public async Task Prevents_description_edits_while_an_external_operation_is_pending()
    {
        // Arrange
        var image = new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "The cyclists are riding through a mountain valley.",
            ResponsiveVariants = [],
            RequiresHumanReview = true
        };
        catalogApi.Images = [image];
        var pendingDraft = new TaskCompletionSource<HttpOutcome<CatalogMediaImageDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
        catalogApi.AccessibilityDraftTask = pendingDraft.Task;
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));

        // Act
        var generate = cut.Find("button.btn-outline-primary").ClickAsync(new MouseEventArgs());
        cut.WaitForState(() => cut.Find("#language").HasAttribute("disabled"));

        var descriptionDisabledWhilePending = cut.Find("#altText").HasAttribute("disabled");
        var pendingDescription = cut.Find("#altText").GetAttribute("value");
        pendingDraft.SetResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, image));
        await generate;

        // Assert
        descriptionDisabledWhilePending.ShouldBeTrue();
        pendingDescription.ShouldBe(image.AltText);
        cut.Find("#altText").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Reviews_the_pending_portuguese_text_without_overwriting_english()
    {
        // Arrange
        catalogApi.Images = [new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(), AltText = "Approved English", ResponsiveVariants = [],
            AccessibilityTexts = [new PublicMediaAccessibilityTextDto
            {
                Language = PublicContentLanguageDto.PtBr, AltText = "Ciclistas atravessam as montanhas.",
                RequiresHumanReview = true, IsAiGenerated = true
            }]
        }];

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.Find("#altText").Change("Ciclistas na montanha.");
        cut.Find("#caption").Change("Passeio de bicicleta");
        cut.Find("button.btn-primary").Click();

        // Assert
        var request = catalogApi.LastAccessibilityReviewRequest.ShouldNotBeNull();
        request.Language.ShouldBe(PublicContentLanguageDto.PtBr);
        request.AltText.ShouldBe("Ciclistas na montanha.");
        request.Caption.ShouldBe("Passeio de bicicleta");
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
        catalogApi.AccessibilityReviewResult = image with
        {
            RequiresHumanReview = false
        };

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
    public void Shows_a_safe_error_when_the_image_list_is_malformed()
    {
        // Arrange
        catalogApi.GetTourImagesOutcome = HttpOutcomeMapper.Status<CatalogMediaImageDto[]>(
            HttpOutcomeKind.MalformedBody,
            HttpStatusCode.OK,
            "The catalog image response body was invalid.");

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(
            () => cut.Markup.Contains("Images could not be loaded.", StringComparison.Ordinal),
            TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldBe("Images could not be loaded. Try again later.");
        cut.Markup.ShouldNotContain("The catalog image response body was invalid.", StringComparison.Ordinal);
        cut.FindAll("img").ShouldBeEmpty();
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
    public void Shows_fixed_copy_instead_of_server_validation_text_when_approval_is_rejected()
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
        var hostile = "<script>alert(1)</script> at Server.Internal.Handler() in /src/Secret.cs:line 42 " + new string('x', 5000);
        catalogApi.ValidationException = new ContractValidationException(
            "Validation failed.",
            new Dictionary<string, string[]>
            {
                ["altText"] = [hostile]
            });

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.FindAll("button.btn-primary").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find("button.btn-primary").Click();
        cut.WaitForState(() => cut.FindAll("[role='alert']").Count == 1, TimeSpan.FromSeconds(2));

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldBe("Accessibility text could not be saved. Try again later.");
        cut.Markup.ShouldNotContain("alert(1)", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Secret.cs", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_not_found_copy_and_keeps_the_current_text_when_the_draft_cannot_be_generated()
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
        catalogApi.Draft = null;
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.WaitForState(() => cut.FindAll("button.btn-outline-primary").Count == 1, TimeSpan.FromSeconds(2));

        // Act
        cut.Find("button.btn-outline-primary").Click();

        // Assert
        cut.WaitForState(() => cut.FindAll("[role='alert']").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find("[role='alert']").TextContent.ShouldBe("Image was not found.");
        cut.Find("#altText").GetAttribute("value").ShouldBe("Cyclists crossing a mountain pass.");
    }

    [Theory]
    [InlineData("image/avif", "avif")]
    [InlineData("image/png", "png")]
    [InlineData("image/webp", "webp")]
    [InlineData(" IMAGE/JPEG ", "jpg")]
    public void Previews_each_supported_image_type_with_its_rendition_format(string contentType, string expectedFormat)
    {
        // Arrange
        var image = new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Cyclists crossing a mountain pass.",
            ResponsiveVariants = [new CatalogMediaImageVariantDto { Width = 320, Height = 200, ContentType = contentType, FileSizeBytes = 320 }],
            RequiresHumanReview = true
        };
        catalogApi.Images = [image];

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));

        // Assert
        cut.WaitForState(() => cut.FindAll("img").Count == 1, TimeSpan.FromSeconds(2));
        cut.Find("img").GetAttribute("src").ShouldBe($"/catalog/media/images/{image.Id}/preview/320/{expectedFormat}");
    }

    [Fact]
    public void Omits_the_preview_for_an_unsupported_image_type()
    {
        // Arrange
        catalogApi.Images =
        [
            new CatalogMediaImageDto
            {
                Id = Guid.CreateVersion7(),
                AltText = "Cyclists crossing a mountain pass.",
                ResponsiveVariants = [new CatalogMediaImageVariantDto { Width = 320, Height = 200, ContentType = "image/gif", FileSizeBytes = 320 }],
                RequiresHumanReview = true
            }
        ];

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));

        // Assert
        cut.WaitForState(() => cut.FindAll("#altText").Count == 1, TimeSpan.FromSeconds(2));
        cut.FindAll("img").ShouldBeEmpty();
    }
}
