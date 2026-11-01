using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class EfPublicContentStoreTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Invalid_lookup_keys_still_honor_cancellation(string key)
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicContentStore(dbContext);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> read = () => store.GetContent(key, cancellation.Token);

        // Assert
        await read.ShouldThrow<OperationCanceledException>();
    }

    [Theory]
    [InlineData("Home.Hero", "HOME.HERO")]
    [InlineData("home.i\u0307", "HOME.I\u0307")]
    public async Task Store_materializes_the_same_canonical_identity_after_tracking_is_cleared(string input, string expected)
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicContentStore(dbContext);
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false, key: input);
        await store.SaveContent(content, TestContext.Current.CancellationToken);
        dbContext.ChangeTracker.Clear();

        // Act
        var loaded = await store.GetContent(content.ContentKey, TestContext.Current.CancellationToken);

        // Assert
        loaded.ShouldNotBeNull();
        loaded.ContentKey.ShouldBe(content.ContentKey);
        loaded.Key.ShouldBe(expected);
        loaded.Id.ShouldBe(content.Id);
    }

    [Fact]
    public async Task Store_persists_and_loads_content_by_sanitized_key()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicContentStore(dbContext);
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);

        // Act
        await store.SaveContent(content, TestContext.Current.CancellationToken);
        var saved = await store.GetContent($"  {content.Key}  ", TestContext.Current.CancellationToken);

        // Assert
        _ = saved.ShouldNotBeNull();
        saved.Key.ShouldBe(content.Key);
        saved.SourceLanguage.ShouldBe(content.SourceLanguage);
        saved.Variants.OrderBy(variant => variant.Language).ShouldBe(content.Variants.OrderBy(variant => variant.Language));
        saved.PublicationState.ShouldBe(content.PublicationState);
    }

    [Fact]
    public async Task Store_replaces_existing_content_with_the_same_key()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicContentStore(dbContext);
        var original = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);
        var replacement = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);

        // Act
        await store.SaveContent(original, TestContext.Current.CancellationToken);
        await store.SaveContent(replacement, TestContext.Current.CancellationToken);
        var saved = await store.GetContent(replacement.Key, TestContext.Current.CancellationToken);

        // Assert
        _ = saved.ShouldNotBeNull();
        saved.Id.ShouldBe(original.Id);
        saved.PublicationState.ShouldBe(replacement.PublicationState);
    }

    [Fact]
    public async Task Store_preserves_published_state_when_replacement_is_published()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicContentStore(dbContext);
        var original = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: true);
        var replacement = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false);
        var publish = replacement.Publish();
        publish.IsSuccess.ShouldBeTrue();

        // Act
        await store.SaveContent(original, TestContext.Current.CancellationToken);
        await store.SaveContent(replacement, TestContext.Current.CancellationToken);
        var saved = await store.GetContent(replacement.Key, TestContext.Current.CancellationToken);

        // Assert
        _ = saved.ShouldNotBeNull();
        saved.PublicationState.ShouldBe(replacement.PublicationState);
    }

    [Fact]
    public async Task Store_matches_keys_case_insensitively_through_canonical_casing()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicContentStore(dbContext);
        var content = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false, key: "Home.Hero");

        // Act
        await store.SaveContent(content, TestContext.Current.CancellationToken);
        var saved = await store.GetContent("HOME.HERO", TestContext.Current.CancellationToken);

        // Assert
        _ = saved.ShouldNotBeNull();
        saved.Id.ShouldBe(content.Id);
    }

    [Fact]
    public async Task Store_lists_content_ordered_by_key()
    {
        // Arrange
        await using var dbContext = EfPublicContentStoreTestDbContextFactory.Create();
        var store = new EfPublicContentStore(dbContext);
        var second = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false, key: "section-b");
        var first = EditablePublicContentTestFactory.CreateContent(requiresHumanReview: false, key: "section-a");

        // Act
        await store.SaveContent(second, TestContext.Current.CancellationToken);
        await store.SaveContent(first, TestContext.Current.CancellationToken);
        var saved = await store.ListContent(TestContext.Current.CancellationToken);

        // Assert
        saved.ShouldMatchCollection(content => content.Key.ShouldBe(first.Key), content => content.Key.ShouldBe(second.Key));
    }

}
