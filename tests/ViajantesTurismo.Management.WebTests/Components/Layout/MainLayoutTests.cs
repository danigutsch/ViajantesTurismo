using Microsoft.AspNetCore.Components;
using PandaBear.Blazor;
using ViajantesTurismo.Management.Web.Components.Layout;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Layout;

public sealed class MainLayoutTests : BunitContext
{
    private readonly RecordingUiFailureReporter reporter = new();

    public MainLayoutTests()
    {
        Services.AddSingleton<IUiFailureReporter>(reporter);
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", true));
    }

    [Fact]
    public void Shows_fixed_recovery_copy_and_reports_the_fault_when_page_content_throws()
    {
        // Act
        var cut = Render<MainLayout>(parameters => parameters.Add<ThrowingContent>(layout => layout.Body));

        // Assert
        var alert = cut.Find("article.content [role=alert]");
        alert.TextContent.ShouldContain("Something went wrong on this page", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain(ThrowingContent.SecretDetail, StringComparison.Ordinal);
        cut.Find("div.sidebar").ShouldNotBeNull();
        reporter.Reports.ShouldBe([(nameof(InvalidOperationException), "management.layout.content")]);
    }

    [Fact]
    public void Shows_the_next_page_after_navigating_away_from_a_failed_page()
    {
        // Arrange
        var cut = Render<MainLayout>(parameters => parameters.Add<ThrowingContent>(layout => layout.Body));
        cut.Find("article.content [role=alert]").ShouldNotBeNull();

        // Act
        cut.Render(parameters => parameters.Add(layout => layout.Body, "Healthy page"));
        var stillFailedBeforeNavigation = cut.FindAll("article.content [role=alert]").Count;
        Services.GetRequiredService<NavigationManager>().NavigateTo("/another-page");

        // Assert
        stillFailedBeforeNavigation.ShouldBe(1);
        cut.FindAll("article.content [role=alert]").ShouldBeEmpty();
        cut.Find("article.content").TextContent.ShouldContain("Healthy page", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_page_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var pageDiv = cut.Find("div.page");
        _ = pageDiv.ShouldNotBeNull();
    }

    [Fact]
    public void Renders_sidebar()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var sidebar = cut.Find("div.sidebar");
        _ = sidebar.ShouldNotBeNull();
    }

    [Fact]
    public void Renders_NavMenu_component()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var navMenu = cut.FindComponent<NavMenu>();
        _ = navMenu.ShouldNotBeNull();
    }

    [Fact]
    public void Renders_main_element()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var main = cut.Find("main");
        _ = main.ShouldNotBeNull();
    }

    [Fact]
    public void Renders_top_row_with_about_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var topRow = cut.Find("main div.top-row");
        _ = topRow.ShouldNotBeNull();
        topRow.TextContent.ShouldContain("About", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_article_content_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var article = cut.Find("article.content");
        _ = article.ShouldNotBeNull();
    }

    [Fact]
    public void Renders_error_UI_container()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        _ = errorUi.ShouldNotBeNull();
    }

    [Fact]
    public void Error_UI_contains_error_message()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        errorUi.TextContent.ShouldContain("An unhandled error has occurred.", StringComparison.Ordinal);
    }

    [Fact]
    public void Error_UI_contains_reload_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        var reloadLink = errorUi.QuerySelector("a.reload");
        _ = reloadLink.ShouldNotBeNull();
        reloadLink.TextContent.ShouldBe("Reload");
        reloadLink.GetAttribute("href").ShouldBe("");
    }

    [Fact]
    public void Error_UI_contains_dismiss_link()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var errorUi = cut.Find("div#blazor-error-ui");
        var dismissLink = errorUi.QuerySelector("a.dismiss");
        _ = dismissLink.ShouldNotBeNull();
        dismissLink.TextContent.ShouldBe("🗙");
    }
}
