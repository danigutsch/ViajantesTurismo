using ViajantesTurismo.Management.Web.Components.Layout;

namespace ViajantesTurismo.Management.WebTests.Components.Layout;

public sealed class MainLayoutTests : BunitContext
{
    public MainLayoutTests()
    {
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", true));
    }

    [Fact]
    public void Renders_page_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var pageDiv = cut.Find("div.page");
        _ = (pageDiv).ShouldNotBeNull();
    }

    [Fact]
    public void Renders_sidebar()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var sidebar = cut.Find("div.sidebar");
        _ = (sidebar).ShouldNotBeNull();
    }

    [Fact]
    public void Renders_NavMenu_component()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var navMenu = cut.FindComponent<NavMenu>();
        _ = (navMenu).ShouldNotBeNull();
    }

    [Fact]
    public void Renders_main_element()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var main = cut.Find("main");
        _ = (main).ShouldNotBeNull();
    }

    [Fact]
    public void Renders_top_row_with_about_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var topRow = cut.Find("main div.top-row");
        _ = (topRow).ShouldNotBeNull();
        (topRow.TextContent).ShouldContain("About", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_article_content_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var article = cut.Find("article.content");
        _ = (article).ShouldNotBeNull();
    }

    [Fact]
    public void Renders_error_UI_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        _ = (errorUi).ShouldNotBeNull();
    }

    [Fact]
    public void Error_UI_contains_error_message()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        (errorUi.TextContent).ShouldContain("An unhandled error has occurred.", StringComparison.Ordinal);
    }

    [Fact]
    public void Error_UI_contains_reload_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        var reloadLink = errorUi.QuerySelector("a.reload");
        _ = (reloadLink).ShouldNotBeNull();
        (reloadLink.TextContent).ShouldBe("Reload");
        (reloadLink.GetAttribute("href")).ShouldBe("");
    }

    [Fact]
    public void Error_UI_contains_dismiss_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        var dismissLink = errorUi.QuerySelector("a.dismiss");
        _ = (dismissLink).ShouldNotBeNull();
        (dismissLink.TextContent).ShouldBe("🗙");
    }
}
