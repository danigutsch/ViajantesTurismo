using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using ReviewImageAccessibility = ViajantesTurismo.Management.Web.Components.Pages.Catalog.ReviewImageAccessibility;
using Microsoft.AspNetCore.Routing;
using PandaBear.HttpClients;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class ReviewImageAccessibilityPageTests : BunitContext
{
    private readonly FakeCatalogToursApiClient catalogApi = new();

    public ReviewImageAccessibilityPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICatalogToursApiClient>(catalogApi);
        Services.AddRouting();
        Services.AddSingleton(new ViajantesTurismo.Management.Web.Services.MediaLanguageGuidance(new ConfigurationBuilder().Build()));
        Services.AddSingleton<EndpointDataSource>(new DefaultEndpointDataSource(
            new RouteEndpoint(_ => Task.CompletedTask,
                RoutePatternFactory.Parse("/catalog/media/images/{id}/preview/{width}/{format}"), 0,
                new EndpointMetadataCollection(new EndpointNameMetadata(ManagementWebEndpoints.MediaPreviewByRenditionEndpointName)), "media")));
    }

    [Fact(Skip = "Requires explicitly provisioned local fastText executable and model.", SkipUnless = nameof(MediaLanguageGuidanceTests.IsLocalEngineConfigured), SkipType = typeof(MediaLanguageGuidanceTests))]
    public void Shows_language_mismatch_as_advisory_and_allows_human_override()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MediaLanguageIdentification:ExecutablePath"] = Environment.GetEnvironmentVariable("MEDIA_TEST_FASTTEXT"),
            ["MediaLanguageIdentification:ModelPath"] = Environment.GetEnvironmentVariable("MEDIA_TEST_FASTTEXT_MODEL")
        }).Build();
        Services.AddSingleton(new ViajantesTurismo.Management.Web.Services.MediaLanguageGuidance(configuration));
        catalogApi.Images = [new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(), AltText = "Um grupo de ciclistas atravessa uma estrada nas montanhas ao lado de um vale verde e tranquilo.",
            ResponsiveVariants = [], RequiresHumanReview = true
        }];

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.Find("button.btn-outline-secondary").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("differs from the declaration", StringComparison.Ordinal));
        cut.WaitForState(() => !cut.Find("button.btn-primary").HasAttribute("disabled"));
        cut.Find("button.btn-primary").Click();
        cut.WaitForState(() => catalogApi.LastAccessibilityReviewRequest is not null);

        // Assert
        var request = catalogApi.LastAccessibilityReviewRequest.ShouldNotBeNull();
        request.Language.ShouldBe(PublicContentLanguageDto.EnUs);
        request.AltText.ShouldBe("Um grupo de ciclistas atravessa uma estrada nas montanhas ao lado de um vale verde e tranquilo.");
    }

    [Fact]
    public void Allows_human_approval_after_a_language_checker_configuration_failure()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MediaLanguageIdentification:ExecutablePath"] = "relative-fasttext",
            ["MediaLanguageIdentification:ModelPath"] = "relative-model"
        }).Build();
        Services.AddSingleton(new ViajantesTurismo.Management.Web.Services.MediaLanguageGuidance(configuration));
        catalogApi.Images = [new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(), AltText = "The cyclists are riding bicycles through the beautiful mountain valley.",
            ResponsiveVariants = [], RequiresHumanReview = true
        }];

        // Act
        var cut = Render<ReviewImageAccessibility>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));
        cut.Find("button.btn-outline-secondary").Click();
        var checkerError = cut.Find("[role='alert']").TextContent;
        cut.Find("button.btn-primary").Click();

        // Assert
        checkerError.ShouldBe("The local language checker could not run. Ask an administrator to check its configuration. You can still approve your text.");
        checkerError.ShouldNotContain("relative-fasttext", StringComparison.Ordinal);
        catalogApi.LastAccessibilityReviewRequest.ShouldNotBeNull();
        cut.Find("button.btn-primary").HasAttribute("disabled").ShouldBeFalse();
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
        cut.Find("button.btn-outline-secondary").Click();
        cut.Find("button.btn-primary").Click();

        // Assert
        var request = catalogApi.LastAccessibilityReviewRequest.ShouldNotBeNull();
        request.Language.ShouldBe(PublicContentLanguageDto.PtBr);
        request.AltText.ShouldBe("Ciclistas na montanha.");
        request.Caption.ShouldBe("Passeio de bicicleta");
        cut.Markup.ShouldContain("indeterminate", StringComparison.Ordinal);
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
    public void Shows_a_safe_error_when_the_image_list_is_malformed()
    {
        // Arrange
        catalogApi.GetTourImagesOutcome = HttpOutcomeMapper.Status<CatalogMediaImageDto[]>(
            HttpOutcomeKind.MalformedBody,
            System.Net.HttpStatusCode.OK,
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
