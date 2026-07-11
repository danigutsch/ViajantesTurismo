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
        _ = TestAssert.NotNull(pageDiv);
    }

    [Fact]
    public void Renders_sidebar()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var sidebar = cut.Find("div.sidebar");
        _ = TestAssert.NotNull(sidebar);
    }

    [Fact]
    public void Renders_NavMenu_component()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var navMenu = cut.FindComponent<NavMenu>();
        _ = TestAssert.NotNull(navMenu);
    }

    [Fact]
    public void Renders_main_element()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var main = cut.Find("main");
        _ = TestAssert.NotNull(main);
    }

    [Fact]
    public void Renders_top_row_with_about_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var topRow = cut.Find("main div.top-row");
        _ = TestAssert.NotNull(topRow);
        TestAssert.Contains("About", topRow.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_article_content_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var article = cut.Find("article.content");
        _ = TestAssert.NotNull(article);
    }

    [Fact]
    public void Renders_error_UI_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        _ = TestAssert.NotNull(errorUi);
    }

    [Fact]
    public void Error_UI_contains_error_message()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        TestAssert.Contains("An unhandled error has occurred.", errorUi.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_UI_contains_reload_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        var reloadLink = errorUi.QuerySelector("a.reload");
        _ = TestAssert.NotNull(reloadLink);
        TestAssert.Equal("Reload", reloadLink.TextContent);
        TestAssert.Equal("", reloadLink.GetAttribute("href"));
    }

    [Fact]
    public void Error_UI_contains_dismiss_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        var dismissLink = errorUi.QuerySelector("a.dismiss");
        _ = TestAssert.NotNull(dismissLink);
        TestAssert.Equal("🗙", dismissLink.TextContent);
    }
}
