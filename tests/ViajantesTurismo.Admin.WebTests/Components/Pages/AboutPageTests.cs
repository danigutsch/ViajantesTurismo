using ViajantesTurismo.Admin.Web.Components.Pages;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages;

public sealed class AboutPageTests : BunitContext
{
    [Fact]
    public void Renders_Static_About_Content_And_Dashboard_Link()
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
