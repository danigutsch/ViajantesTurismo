using ViajantesTurismo.Admin.Web.Components.Layout;

namespace ViajantesTurismo.Admin.WebTests.Components.Layout;

public sealed class MainLayoutTests : BunitContext
{
    [Fact]
    public void Renders_Page_Container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var pageDiv = cut.Find("div.page");
        Assert.NotNull(pageDiv);
    }

    [Fact]
    public void Renders_Sidebar()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var sidebar = cut.Find("div.sidebar");
        Assert.NotNull(sidebar);
    }

    [Fact]
    public void Renders_NavMenu_Component()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var navMenu = cut.FindComponent<NavMenu>();
        Assert.NotNull(navMenu);
    }

    [Fact]
    public void Renders_Main_Element()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var main = cut.Find("main");
        Assert.NotNull(main);
    }

    [Fact]
    public void Renders_Top_Row_With_About_Link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var topRow = cut.Find("main div.top-row");
        Assert.NotNull(topRow);
        Assert.Contains("About", topRow.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Article_Content_Container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var article = cut.Find("article.content");
        Assert.NotNull(article);
    }

    [Fact]
    public void Renders_Error_UI_Container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        Assert.NotNull(errorUi);
    }

    [Fact]
    public void Error_UI_Contains_Error_Message()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        Assert.Contains("An unhandled error has occurred.", errorUi.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_UI_Contains_Reload_Link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        var reloadLink = errorUi.QuerySelector("a.reload");
        Assert.NotNull(reloadLink);
        Assert.Equal("Reload", reloadLink.TextContent);
        Assert.Equal("", reloadLink.GetAttribute("href"));
    }

    [Fact]
    public void Error_UI_Contains_Dismiss_Link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        var dismissLink = errorUi.QuerySelector("a.dismiss");
        Assert.NotNull(dismissLink);
        Assert.Equal("🗙", dismissLink.TextContent);
    }
}
