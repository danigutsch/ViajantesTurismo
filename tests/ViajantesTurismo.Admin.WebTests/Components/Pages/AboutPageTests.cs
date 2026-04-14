using ViajantesTurismo.Admin.Web.Components.Pages;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages;

public sealed class AboutPageTests : BunitContext
{
    [Fact]
    public void Renders_Production_When_AspNetCoreEnvironment_Is_Not_Set()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

        try
        {
            // Act
            var cut = Render<About>();

            // Assert
            Assert.Contains("About ViajantesTurismo Admin Portal", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("<td>Production</td>", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("support@viajantesturismo.example", cut.Markup, StringComparison.Ordinal);
            Assert.Equal("/", cut.Find("a.btn.btn-primary").GetAttribute("href"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalValue);
        }
    }

    [Fact]
    public void Renders_Configured_AspNetCoreEnvironment_When_Set()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        try
        {
            // Act
            var cut = Render<About>();

            // Assert
            Assert.Contains("<td>Development</td>", cut.Markup, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalValue);
        }
    }
}
