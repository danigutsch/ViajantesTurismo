using ViajantesTurismo.Management.Web.Components.Pages;

namespace ViajantesTurismo.Management.WebTests.Components.Pages;

public sealed class AboutPageTests : BunitContext
{
    [Fact]
    public void Renders_static_about_content_and_dashboard_link()
    {
        // Act
        var cut = Render<About>();

        // Assert
        Assert.Contains("About ViajantesTurismo Admin Portal", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Application Information", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("support@viajantesturismo.example", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Back to Dashboard", cut.Markup, StringComparison.Ordinal);
        Assert.Equal("/", cut.Find("a.btn.btn-primary").GetAttribute("href"));
    }
}
