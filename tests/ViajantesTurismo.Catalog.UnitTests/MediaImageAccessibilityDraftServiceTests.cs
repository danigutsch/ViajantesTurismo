using SharedKernel.AI;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(SharedKernel.Testing.SharedKernelTestTraitNames.ScopeName, SharedKernel.Testing.SharedKernelTestTraitNames.UnitScope)]
[Trait(SharedKernel.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.AiAccessibilityCapability)]
public sealed class MediaImageAccessibilityDraftServiceTests
{
    [Fact]
    public async Task Generate_draft_returns_invalid_when_image_id_is_empty()
    {
        // Arrange
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7()));
        var objectStore = new InMemoryMediaObjectStore();
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            Guid.Empty,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_invalid_when_language_is_missing()
    {
        // Arrange
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7()));
        var objectStore = new InMemoryMediaObjectStore();
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            Guid.CreateVersion7(),
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.None },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_stores_ai_text_that_requires_human_review()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7(), altText: "Existing alt");
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(
                image.SourceObjectKey,
                new MemoryStream([1, 2, 3]),
                "image/jpeg",
                3,
                "sha256:abc"),
            TestContext.Current.CancellationToken);
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft beach alt", "Draft beach caption"));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput
            {
                Language = PublicContentLanguage.EnUs,
                Context = "Homepage hero",
                Latitude = -23.55m,
                Longitude = -46.63m
            },
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        imageStore.Current.AltText.ShouldBe("Draft beach alt");
        imageStore.Current.Caption.ShouldBe("Draft beach caption");
        imageStore.Current.IsAiGenerated.ShouldBeTrue();
        imageStore.Current.RequiresHumanReview.ShouldBeTrue();

        var draft = imageStore.Current.AccessibilityTexts.Single();
        draft.AltText.ShouldBe("Draft beach alt");
        draft.Caption.ShouldBe("Draft beach caption");
        draft.IsAiGenerated.ShouldBeTrue();
        draft.RequiresHumanReview.ShouldBeTrue();

        generator.Request.ShouldNotBeNull();
        generator.Request.ContentType.ShouldBe("image/jpeg");
        generator.Request.Language.ShouldBe("en-US");
        generator.Request.Context.ShouldBe("Homepage hero");
        generator.Request.Latitude.ShouldBe(-23.55m);
        generator.Request.Longitude.ShouldBe(-46.63m);
    }

    [Fact]
    public async Task Generate_draft_returns_not_found_when_image_is_missing()
    {
        // Arrange
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7()));
        var objectStore = new InMemoryMediaObjectStore();
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            Guid.CreateVersion7(),
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_not_found_when_source_object_is_missing()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_unavailable_when_source_object_cannot_be_opened()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        objectStore.FailNextOpen(new IOException("Source unreadable."));
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Unavailable);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_not_found_when_source_file_is_missing()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        objectStore.FailNextOpen(new FileNotFoundException("Source missing."));
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_not_found_when_source_directory_is_missing()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        objectStore.FailNextOpen(new DirectoryNotFoundException("Source directory missing."));
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_unavailable_when_source_object_access_is_denied()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        objectStore.FailNextOpen(new UnauthorizedAccessException("Source denied."));
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Unavailable);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_invalid_when_location_is_partial()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs, Latitude = -23.55m },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_unavailable_when_generator_fails()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/jpeg", 1, "sha256:abc"),
            TestContext.Current.CancellationToken);
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        generator.Throw(new ImageTextGenerationException("Proxy failed."));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Unavailable);
        imageStore.Current.IsAiGenerated.ShouldBeFalse();
    }

    [Fact]
    public async Task Generate_draft_returns_unavailable_when_generator_is_misconfigured()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/jpeg", 1, "sha256:abc"),
            TestContext.Current.CancellationToken);
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        generator.Throw(new InvalidOperationException("Missing model."));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Unavailable);
        imageStore.Current.IsAiGenerated.ShouldBeFalse();
    }

    [Fact]
    public async Task Generate_draft_returns_invalid_when_generated_alt_text_is_empty()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/jpeg", 1, "sha256:abc"),
            TestContext.Current.CancellationToken);
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult(string.Empty, null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        imageStore.Current.IsAiGenerated.ShouldBeFalse();
    }

    [Fact]
    public async Task Generate_draft_uses_pt_br_language_tag()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/jpeg", 1, "sha256:abc"),
            TestContext.Current.CancellationToken);
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Rascunho", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentLanguage.PtBr },
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        generator.Request.ShouldNotBeNull();
        generator.Request.Language.ShouldBe("pt-BR");
    }

    [Fact]
    public async Task Generate_draft_uses_undetermined_language_tag_for_unknown_language()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = new InMemoryMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/jpeg", 1, "sha256:abc"),
            TestContext.Current.CancellationToken);
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = (PublicContentLanguage)999 },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        generator.Request.ShouldNotBeNull();
        generator.Request.Language.ShouldBe("und");
    }
}
