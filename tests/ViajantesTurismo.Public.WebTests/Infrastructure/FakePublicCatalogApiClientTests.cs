using SharedKernel.HttpClients;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[TestScope(TestScope.Unit)]
public sealed class FakePublicCatalogApiClientTests
{
    [Fact]
    public void Fake_public_catalog_content_requires_a_culture()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        var content = new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.EnglishUnitedStates,
            Title = "Cycle safely",
            Body = "Guided tours for everyone.",
            SeoTitle = "Cycle safely - Viajantes Turismo"
        };

        // Act
        Action act = () => catalogApi.AddContent(" ", content);
        var exception = act.ShouldThrow<ArgumentException>();

        // Assert
        exception.ParamName.ShouldBe("culture");
    }

    [Fact]
    public async Task Fake_public_catalog_content_keeps_newline_keys_distinct()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("home\nhero", "en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.EnglishUnitedStates,
            Title = "Newline key",
            Body = "Key-specific content.",
            SeoTitle = "Newline key - Viajantes Turismo"
        });
        catalogApi.AddContent("home", "hero\nen-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.EnglishUnitedStates,
            Title = "Other key",
            Body = "Other content.",
            SeoTitle = "Other key - Viajantes Turismo"
        });

        // Act
        var outcome = await catalogApi.GetPublicContent(
            "home\nhero",
            "en-US",
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var content = outcome.Value.ShouldNotBeNull();
        content.Title.ShouldBe("Newline key");
    }
}
