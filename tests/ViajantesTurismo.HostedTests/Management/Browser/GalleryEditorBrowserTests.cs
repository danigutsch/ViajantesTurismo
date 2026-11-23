using Microsoft.Playwright;
using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Drives bounded photo search, ordered editing and the exact placement outcome through Management.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class GalleryEditorBrowserTests : ManagementWebBrowserTestBase
{
    private readonly GalleryEditorCatalogStub catalog = new();

    public GalleryEditorBrowserTests()
    {
        GalleryEditorBrowserScenario.Register(Registrations, catalog);
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            catalog.Dispose();
        }
    }

    [Fact]
    public async Task Ordered_gallery_replacement_uses_bounded_search_and_the_exact_queued_outcome()
    {
        // Arrange
        await NavigateTo(catalog.EditorPage);
        var photos = Page.Locator("#gallery-photos");
        var cover = Page.Locator("#gallery-cover");
        var save = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save gallery" });
        await Expect(save).ToBeDisabledAsync();

        // Act
        await photos.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Remove Photo 001", Exact = true }).ClickAsync();
        await photos.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Move Photo 003 earlier", Exact = true }).ClickAsync();
        await Page.Locator("#gallery-photos-find").ClickAsync();
        await Page.Locator("#gallery-photos-search").FillAsync("Photo 803");
        await photos.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Add Photo 803", Exact = true }).ClickAsync();
        await Page.Locator("#gallery-cover-find").ClickAsync();
        await Page.Locator("#gallery-cover-search").FillAsync("Photo 803");
        await cover.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Choose Photo 803", Exact = true }).ClickAsync();
        await save.ClickAsync();

        // Assert
        await Expect(Page.GetByText("The gallery change is queued.", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(save).ToHaveCountAsync(0);
        var write = catalog.WriteRequests.ShouldHaveSingleItem();
        write.StreamVersion.ShouldBe(3L);
        write.AcceptedPlacementSequence.ShouldBe(0L);
        write.PhotoIds.Count.ShouldBe(500);
        write.PhotoIds.Take(2).ShouldBe([catalog.PhotoKey(3), catalog.PhotoKey(2)]);
        write.PhotoIds[^1].ShouldBe(catalog.PhotoKey(803));
        write.CoverId.ShouldBe(catalog.PhotoKey(803));
        catalog.EditorRequests.ShouldContain("?placementSequence=7");
        var searches = catalog.ChoiceRequests.Where(request => request.Query == "Photo 803").ToArray();
        searches.Length.ShouldBe(2);
        searches[0].Limit.ShouldBe(24);
        searches[0].IncludedPhotoIds.ShouldBeNull();
        searches[1].Limit.ShouldBe(24);
        searches[1].IncludedPhotoIds.ShouldNotBeNull().ShouldContain(catalog.PhotoKey(803));
    }
}
