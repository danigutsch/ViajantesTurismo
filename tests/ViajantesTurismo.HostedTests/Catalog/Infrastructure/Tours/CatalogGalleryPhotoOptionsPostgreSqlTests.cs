using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Exercises DB-side photo counts, scope and search against the Catalog migrations.</summary>
[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryPhotoOptionsPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Gallery_options_search_beyond_the_first_page_and_scope_selected_IDs_to_the_tour()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedDraft(authoredGallery: false, ct);
        var photos = Enumerable.Range(0, 803).Select(index => PublicMediaImageTestFactory.CreateReadyImage(
            tour.Id, $"tour-{index}.jpg", $"tour-{index}-640.jpg", $"sha256:tour-{index}",
            index == 801 ? "São Paulo 100%" : $"Photo {index}", index, index == 0)).ToArray();
        var foreign = PublicMediaImageTestFactory.CreateReadyImage(
            Guid.CreateVersion7(), "foreign.jpg", "foreign-640.jpg", "sha256:foreign", "São Paulo 100%", 0, true);
        scenario.Database.PublicMediaImages.AddRange(photos);
        scenario.Database.PublicMediaImages.Add(foreign);
        await scenario.Database.SaveChangesAsync(ct);

        // Act
        var first = await scenario.PhotoOptions.Search(tour.Id, new CatalogGalleryPhotoOptionsRequest(null), ct);
        var byText = await scenario.PhotoOptions.Search(tour.Id, new CatalogGalleryPhotoOptionsRequest("SAO PAULO 100%"), ct);
        var byPercent = await scenario.PhotoOptions.Search(tour.Id, new CatalogGalleryPhotoOptionsRequest("%"), ct);
        var bySelection = await scenario.PhotoOptions.Search(tour.Id, new CatalogGalleryPhotoOptionsRequest(
            null, IncludedPhotoIds: [foreign.Id, photos[100].Id, photos[20].Id]), ct);
        var empty = await scenario.PhotoOptions.Search(tour.Id, new CatalogGalleryPhotoOptionsRequest(null, IncludedPhotoIds: []), ct);

        // Assert
        var page = first.ShouldNotBeNull();
        page.Total.ShouldBe(photos.Length);
        page.Items.Select(photo => photo.Id).ShouldBe(photos.Take(CatalogGalleryPhotoOptionsRequest.MaxPageSize).Select(photo => photo.Id));
        var match = byText.ShouldNotBeNull().Items.ShouldHaveSingleItem();
        match.Id.ShouldBe(photos[801].Id);
        match.Label.ShouldBe("São Paulo 100%");
        match.PreviewWidth.ShouldBe(640);
        match.PreviewContentType.ShouldBe("image/jpeg");
        byPercent.ShouldNotBeNull().Total.ShouldBe(1);
        var selected = bySelection.ShouldNotBeNull();
        selected.Total.ShouldBe(2);
        selected.Items.Select(photo => photo.Id).ShouldBe([photos[20].Id, photos[100].Id]);
        empty.ShouldNotBeNull().Items.ShouldBeEmpty();
    }
}
