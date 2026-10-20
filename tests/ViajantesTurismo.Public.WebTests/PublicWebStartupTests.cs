using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.ScopeName, TestTraits.WebIntegrationScope)]
[Trait(TestTraitNames.AreaName, TestTraits.PublicWebArea)]
public sealed class PublicWebStartupTests
{
    [Fact]
    public async Task Production_host_fails_startup_when_sitemap_canonical_origin_is_missing()
    {
        // Arrange
        var canonicalOrigin = string.Empty;

        // Act
        var result = await PublicWebEndpointTestsHelpers.RunProductionStartup(canonicalOrigin, TestContext.Current.CancellationToken);

        // Assert
        result.ExitCode.ShouldNotBe(0);
        result.Output.ShouldContain("OptionsValidationException", StringComparison.Ordinal);
        result.Output.ShouldContain(
            "PublicWeb:Sitemap:CanonicalOrigin must be provided.",
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_host_fails_startup_when_sitemap_canonical_origin_is_invalid()
    {
        // Arrange
        var canonicalOrigin = "https://public.example.test/sitemap.xml";

        // Act
        var result = await PublicWebEndpointTestsHelpers.RunProductionStartup(
            canonicalOrigin, TestContext.Current.CancellationToken);

        // Assert
        result.ExitCode.ShouldNotBe(0);
        result.Output.ShouldContain("OptionsValidationException", StringComparison.Ordinal);
        result.Output.ShouldContain(
            "PublicWeb:Sitemap:CanonicalOrigin must be an absolute HTTP or HTTPS origin without a path, query, fragment, or userinfo.",
            StringComparison.Ordinal);
    }
}
