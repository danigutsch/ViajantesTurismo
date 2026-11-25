using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests.Hosting;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[Trait(TestTraitNames.AreaName, TestTraits.PublicWebArea)]
[TestScope(TestScope.Integration)]
public sealed class PublicWebStartupTests
{
    [Fact]
    public async Task Production_host_fails_startup_when_sitemap_canonical_origin_is_missing()
    {
        // Arrange
        var canonicalOrigin = string.Empty;

        // Act
        var (ExitCode, Output) = await PublicWebTestHost.RunProductionStartup(canonicalOrigin, TestContext.Current.CancellationToken);

        // Assert
        ExitCode.ShouldNotBe(0);
        Output.ShouldContain("OptionsValidationException", StringComparison.Ordinal);
        Output.ShouldContain(
            "Set PublicWeb:Sitemap:CanonicalOrigin to the site's absolute http or https origin without a path",
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_host_fails_startup_when_sitemap_canonical_origin_is_invalid()
    {
        // Arrange
        var canonicalOrigin = "https://public.example.test/sitemap.xml";

        // Act
        var (ExitCode, Output) = await PublicWebTestHost.RunProductionStartup(
            canonicalOrigin, TestContext.Current.CancellationToken);

        // Assert
        ExitCode.ShouldNotBe(0);
        Output.ShouldContain("OptionsValidationException", StringComparison.Ordinal);
        Output.ShouldContain(
            "Set PublicWeb:Sitemap:CanonicalOrigin to the site's absolute http or https origin without a path",
            StringComparison.Ordinal);
    }
}
