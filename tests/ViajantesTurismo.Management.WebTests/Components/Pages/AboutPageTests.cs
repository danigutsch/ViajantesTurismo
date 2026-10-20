using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Management.Web.Components.Pages;

namespace ViajantesTurismo.Management.WebTests.Components.Pages;

public sealed class AboutPageTests : BunitContext
{
    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, "Regression")]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, SharedKernel.Testing.SharedKernelTestTraitNames.UnitScope)]
    public void Displays_the_effective_host_environment()
    {
        // Arrange
        Services.AddSingleton<IHostEnvironment>(new Infrastructure.MockWebHostEnvironment { EnvironmentName = "TemporalTests" });

        // Act
        var cut = Render<About>();

        // Assert
        cut.Markup.ShouldContain("TemporalTests", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_static_about_content_and_dashboard_link()
    {
        // Arrange
        Services.AddSingleton<IHostEnvironment>(new Infrastructure.MockWebHostEnvironment());

        // Act
        var cut = Render<About>();

        // Assert
        (cut.Markup).ShouldContain("About ViajantesTurismo Admin Portal", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Application Information", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("support@viajantesturismo.example", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Back to Dashboard", StringComparison.Ordinal);
        (cut.Find("a.btn.btn-primary").GetAttribute("href")).ShouldBe("/");
    }
}
