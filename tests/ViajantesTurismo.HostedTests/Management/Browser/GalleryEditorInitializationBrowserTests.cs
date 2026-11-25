using Microsoft.Playwright;
using SharedKernel.Testing;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Verifies the explicit legacy initialization path while a placement waits for the baseline.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class GalleryEditorInitializationBrowserTests : ManagementWebBrowserTestBase
{
    private readonly GalleryEditorCatalogStub catalog = new(requiresInitialization: true);

    public GalleryEditorInitializationBrowserTests()
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
    public async Task Legacy_gallery_with_pending_placement_requires_an_explicit_initialization_click()
    {
        // Arrange
        await NavigateTo(catalog.EditorPage);
        var initialize = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Initialize gallery", Exact = true });
        await Expect(initialize).ToBeEnabledAsync();
        catalog.InitializationCount.ShouldBe(0);
        catalog.ChoiceRequests.ShouldBeEmpty();

        // Act
        await initialize.ClickAsync();

        // Assert
        await Expect(Page.Locator("#gallery-photos")).ToBeVisibleAsync();
        await Expect(Page.Locator("#gallery-cover").GetByText("Photo 500", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(initialize).ToHaveCountAsync(0);
        catalog.InitializationCount.ShouldBe(1);
        catalog.WriteRequests.ShouldBeEmpty();
    }
}
