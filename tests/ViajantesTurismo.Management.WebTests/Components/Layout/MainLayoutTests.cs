using ViajantesTurismo.Management.Web.Components.Layout;

namespace ViajantesTurismo.Management.WebTests.Components.Layout;

public sealed class MainLayoutTests : BunitContext
{
    [Fact]
    public void Renders_page_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var pageDiv = cut.Find("div.page");
        Assert.NotNull(pageDiv);
    }

    [Fact]
    public void Renders_sidebar()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var sidebar = cut.Find("div.sidebar");
        Assert.NotNull(sidebar);
    }

    [Fact]
    public void Renders_navMenu_component()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var navMenu = cut.FindComponent<NavMenu>();
        Assert.NotNull(navMenu);
    }

    [Fact]
    public void Renders_main_element()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var main = cut.Find("main");
        Assert.NotNull(main);
    }

    [Fact]
    public void Renders_top_row_with_about_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var topRow = cut.Find("main div.top-row");
        Assert.NotNull(topRow);
        Assert.Contains("About", topRow.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_article_content_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var article = cut.Find("article.content");
        Assert.NotNull(article);
    }

    [Fact]
    public void Renders_error_uI_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        Assert.NotNull(errorUi);
    }

    [Fact]
    public void Error_uI_contains_error_message()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        Assert.Contains("An unhandled error has occurred.", errorUi.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_uI_contains_reload_link()
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
    public void Error_uI_contains_dismiss_link()
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
