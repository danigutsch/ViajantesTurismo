using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using SharedKernel.AI;
using SharedKernel.Testing.AI;
using SharedKernel.BuildingBlocks;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(TestTraitNames.CapabilityName, TestTraits.AiAccessibilityCapability)]
[Collection(CatalogTelemetryGroup.Name)]
public sealed class MediaImageAccessibilityDraftServiceTests
{
    [Theory]
    [InlineData(false, null, null, null)]
    [InlineData(true, 12L, 7L, "0.0045")]
    [InlineData(true, 0L, null, null)]
    [InlineData(true, null, null, "0")]
    [InlineData(true, -1L, -2L, "-1")]
    public async Task Provider_usage_is_counted_with_only_bounded_token_dimensions(bool hasUsage, long? inputTokens, long? outputTokens, string? reportedCost)
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objects = MediaObjectStoreTestFactory.Create();
        objects.Seed(image.SourceObjectKey, [1], "image/jpeg", "sha256:abc");
        decimal? cost = reportedCost is null ? null : decimal.Parse(reportedCost, CultureInfo.InvariantCulture);
        var generation = new ImageTextGenerationResult("Private generated text", null)
        {
            Usage = hasUsage ? new ImageTextGenerationUsage(inputTokens, outputTokens, cost) : null
        };
        var service = new MediaImageAccessibilityDraftService(imageStore, objects, new TestImageTextGenerator(generation));

        var tokens = new ConcurrentQueue<(long Value, KeyValuePair<string, object?>[] Tags)>();
        var costs = new ConcurrentQueue<(double Value, KeyValuePair<string, object?>[] Tags)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = static (instrument, owner) =>
            {
                if (instrument.Meter.Name == CatalogTelemetry.Name
                    && instrument.Name is "catalog.media_accessibility.generation.tokens" or "catalog.media_accessibility.generation.cost_usd")
                {
                    owner.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => tokens.Enqueue((value, tags.ToArray())));
        listener.SetMeasurementEventCallback<double>((_, value, tags, _) => costs.Enqueue((value, tags.ToArray())));
        listener.Start();

        // Act
        var result = await service.GenerateDraft(image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates, Context = "Private prompt" },
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var expectedTokens = new List<(long Value, object Kind)>();
        if (inputTokens is { } input && input >= 0)
        {
            expectedTokens.Add((input, "input"));
        }
        if (outputTokens is { } output && output >= 0)
        {
            expectedTokens.Add((output, "output"));
        }
        var actualTokens = tokens.Select(static measurement =>
        {
            var tag = measurement.Tags.ShouldHaveSingleItem();
            tag.Key.ShouldBe("gen_ai.token.type");
            return (measurement.Value, tag.Value.ShouldNotBeNull());
        });
        actualTokens.ShouldBe(expectedTokens);
        if (cost is { } usd && usd >= 0)
        {
            var (value, tags) = costs.ShouldHaveSingleItem();
            value.ShouldBe((double)usd);
            tags.ShouldBeEmpty();
        }
        else
        {
            costs.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Generated_draft_records_a_persistable_pending_timestamp()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var store = new InMemoryPublicMediaImageStore(image);
        var objects = MediaObjectStoreTestFactory.Create();
        objects.Seed(image.SourceObjectKey, [1], "image/jpeg", "sha256:abc");
        var generatedAt = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        var service = new MediaImageAccessibilityDraftService(store, objects, new TestImageTextGenerator(new ImageTextGenerationResult("A beach", null)), new FakeTimeProvider(generatedAt));

        // Act
        var result = await service.GenerateDraft(image.Id, new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.PortugueseBrazil }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var draft = store.Current.AccessibilityTexts.ShouldHaveSingleItem(text => text.Language == PublicContentCultures.PortugueseBrazil);
        draft.PendingDraftCreatedAt.ShouldBe(generatedAt);
    }

    [Fact]
    public async Task Generation_attempt_emits_completion_telemetry_without_private_input()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.Seed(image.SourceObjectKey, [1], "image/jpeg", "sha256:abc");
        var provenance = new MediaImageAccessibilityGenerationProvenance("litellm", "configured-model", "artifact-version", "configuration-fingerprint");
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, new TestImageTextGenerator(new ImageTextGenerationResult("A beach", null)), provenance: provenance);
        var activities = new ConcurrentQueue<Activity>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(activities);
        var measurements = new ConcurrentQueue<string>();
        using var listener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);

        // Act
        var result = await service.GenerateDraft(image.Id, new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates, Context = "Private context" }, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        measurements.Contains("catalog.media_accessibility.generation.attempts").ShouldBeTrue();
        measurements.Contains("catalog.media_accessibility.generation.completed").ShouldBeTrue();
        measurements.Contains("catalog.media_accessibility.generation.duration").ShouldBeTrue();
        imageStore.Current.RequiresHumanReview.ShouldBeTrue();
        var generation = activities.ShouldHaveSingleItem(activity => activity.OperationName == "catalog.media_accessibility.generate");
        generation.GetTagItem("catalog.outcome").ShouldBe("success");
        generation.GetTagItem("gen_ai.request.model").ShouldBe("configured-model");
        generation.GetTagItem("gen_ai.response.model").ShouldBe("unreported");
        generation.GetTagItem("catalog.media_accessibility.prompt_version").ShouldBe("artifact-version");
        generation.GetTagItem("catalog.media_accessibility.config_version").ShouldBe("configuration-fingerprint");
        generation.TagObjects.Select(tag => tag.Key).ShouldNotContain("context");
        generation.TagObjects.Any(tag => Equals(tag.Value, "Private context")).ShouldBeFalse();
    }

    [Fact]
    public async Task Cancelled_generation_records_cancelled_outcome_and_preserves_review_safety()
    {
        // Arrange
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.Seed(image.SourceObjectKey, [1], "image/jpeg", "sha256:abc");
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null))
        {
            BeforeResult = _ => new ValueTask(cancellation.CancelAsync()),
        };
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);
        var activities = new ConcurrentQueue<Activity>();
        using var listener = CatalogTelemetryTestsHelpers.CreateActivityListener(activities);

        // Act
        Func<Task> action = () => service.GenerateDraft(image.Id, new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates }, cancellation.Token).AsTask();
        await action.ShouldThrow<TaskCanceledException>();

        // Assert
        var generation = activities.ShouldHaveSingleItem(activity => activity.OperationName == "catalog.media_accessibility.generate");
        generation.GetTagItem("catalog.outcome").ShouldBe("cancelled");
        generator.LastRequest.ShouldNotBeNull();
        imageStore.Current.IsAiGenerated.ShouldBeFalse();
    }

    [Fact]
    public async Task Generate_draft_returns_invalid_when_image_ID_is_empty()
    {
        // Arrange
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7()));
        var objectStore = MediaObjectStoreTestFactory.Create();
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            Guid.Empty,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        generator.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_invalid_when_language_is_missing()
    {
        // Arrange
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7()));
        var objectStore = MediaObjectStoreTestFactory.Create();
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            Guid.CreateVersion7(),
            new MediaImageAccessibilityDraftInput { Language = null },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        generator.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_stores_AI_text_that_requires_human_review()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7(), altText: "Existing alt");
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.Seed(image.SourceObjectKey, [1, 2, 3], "image/jpeg", "sha256:abc");
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft beach alt", "Draft beach caption"));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput
            {
                Language = PublicContentCultures.EnglishUnitedStates,
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

        var draft = imageStore.Current.AccessibilityTexts.ShouldHaveSingleItem();
        draft.AltText.ShouldBe("Draft beach alt");
        draft.Caption.ShouldBe("Draft beach caption");
        draft.IsAiGenerated.ShouldBeTrue();
        draft.RequiresHumanReview.ShouldBeTrue();

        var recordedRequest = generator.LastRequest.ShouldNotBeNull();
        recordedRequest.ContentType.ShouldBe("image/jpeg");
        recordedRequest.Language.ShouldBe("en-US");
        recordedRequest.Context.ShouldBe("Homepage hero");
        recordedRequest.Latitude.ShouldBe(-23.55m);
        recordedRequest.Longitude.ShouldBe(-46.63m);
    }

    [Fact]
    public async Task Generate_draft_returns_not_found_when_image_is_missing()
    {
        // Arrange
        var imageStore = new InMemoryPublicMediaImageStore(PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7()));
        var objectStore = MediaObjectStoreTestFactory.Create();
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            Guid.CreateVersion7(),
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        generator.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_not_found_when_source_object_is_missing()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        generator.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_unavailable_when_source_object_cannot_be_opened()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.FailNext(ObjectStoreOperation.OpenRead, new IOException("Source unreadable."));
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Unavailable);
        generator.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_not_found_when_source_file_is_missing()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.FailNext(ObjectStoreOperation.OpenRead, new FileNotFoundException("Source missing."));
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        generator.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_not_found_when_source_directory_is_missing()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.FailNext(ObjectStoreOperation.OpenRead, new DirectoryNotFoundException("Source directory missing."));
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.NotFound);
        generator.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_unavailable_when_source_object_access_is_denied()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.FailNext(ObjectStoreOperation.OpenRead, new UnauthorizedAccessException("Source denied."));
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Unavailable);
        generator.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_invalid_when_location_is_partial()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates, Latitude = -23.55m },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        generator.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Generate_draft_returns_unavailable_when_generator_fails()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.Seed(image.SourceObjectKey, [1], "image/jpeg", "sha256:abc");
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null)) { Exception = new ImageTextGenerationException("Proxy failed.") };
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
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
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.Seed(image.SourceObjectKey, [1], "image/jpeg", "sha256:abc");
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null)) { Exception = new InvalidOperationException("Missing model.") };
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
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
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.Seed(image.SourceObjectKey, [1], "image/jpeg", "sha256:abc");
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult(string.Empty, null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.EnglishUnitedStates },
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
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.Seed(image.SourceObjectKey, [1], "image/jpeg", "sha256:abc");
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Rascunho", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = PublicContentCultures.PortugueseBrazil },
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var recordedRequest = generator.LastRequest.ShouldNotBeNull();
        recordedRequest.Language.ShouldBe("pt-BR");
    }

    [Fact]
    public async Task Generate_draft_uses_undetermined_language_tag_for_unknown_language()
    {
        // Arrange
        var image = PublicMediaImageTestFactory.CreateImage(Guid.CreateVersion7(), 0, true, imageId: Guid.CreateVersion7());
        var imageStore = new InMemoryPublicMediaImageStore(image);
        var objectStore = MediaObjectStoreTestFactory.Create();
        objectStore.Seed(image.SourceObjectKey, [1], "image/jpeg", "sha256:abc");
        var generator = new TestImageTextGenerator(new ImageTextGenerationResult("Draft alt", null));
        var service = new MediaImageAccessibilityDraftService(imageStore, objectStore, generator);

        // Act
        var result = await service.GenerateDraft(
            image.Id,
            new MediaImageAccessibilityDraftInput { Language = ContentCulture.Restore("fr-FR") },
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(SharedKernel.Results.ResultStatus.Invalid);
        generator.LastRequest.ShouldBeNull();
    }
}
