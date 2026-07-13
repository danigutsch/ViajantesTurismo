using ViajantesTurismo.Management.Web.Components.Layout;

namespace ViajantesTurismo.Management.WebTests.Components.Layout;

public sealed class MainLayoutInteractivityTests : BunitContext
{
    [Fact]
    public void Prerendered_page_blocks_interaction_until_the_renderer_is_interactive()
    {
        // Arrange
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", false));

        // Act
        var cut = Render<MainLayout>();

        // Assert
        var page = cut.Find("div.page");
        (page.GetAttribute("inert")).ShouldBe("inert");
        (page.GetAttribute("aria-busy")).ShouldBe("true");
    }
}
