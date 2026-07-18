using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(SharedKernel.Testing.SharedKernelTestTraitNames.ScopeName, SharedKernel.Testing.SharedKernelTestTraitNames.UnitScope)]
public sealed class PublicContentUpsertServiceTests
{
    [Fact]
    public async Task Upsert_saves_canonical_content_that_requires_review()
    {
        // Arrange
        var store = new TestPublicContentStore();
        var service = new PublicContentUpsertService(store);
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.EnUs,
            Title = "Welcome",
            Body = "Ride with us"
        });
        request.Variants.Add(new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.PtBr,
            Title = "Bem-vindo",
            Body = "Pedale conosco",
            RequiresHumanReview = true
        });

        // Act
        var result = await service.Upsert(" home.hero ", request, TestContext.Current.CancellationToken);
        var saved = await store.GetContent("HOME.HERO", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Key.ShouldBe("HOME.HERO");
        result.Value.PublicationState.ShouldBe(PublicContentPublicationState.ReviewRequired);
        saved.ShouldNotBeNull();
        saved.Key.ShouldBe("HOME.HERO");
        saved.PublicationState.ShouldBe(PublicContentPublicationState.ReviewRequired);
    }
}
