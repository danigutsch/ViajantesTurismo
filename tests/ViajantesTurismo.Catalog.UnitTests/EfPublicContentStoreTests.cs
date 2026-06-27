using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class EfPublicContentStoreTests
{
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
        Assert.NotNull(saved);
        Assert.Equal(content.Key, saved.Key);
        Assert.Equal(content.SourceLanguage, saved.SourceLanguage);
        Assert.Equal(content.EnUs, saved.EnUs);
        Assert.Equal(content.PtBr, saved.PtBr);
        Assert.Equal(content.PublicationState, saved.PublicationState);
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
        Assert.NotNull(saved);
        Assert.Equal(original.Id, saved.Id);
        Assert.Equal(replacement.PublicationState, saved.PublicationState);
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
        Assert.Collection(
            saved,
            content => Assert.Equal(first.Key, content.Key),
            content => Assert.Equal(second.Key, content.Key));
    }

}
