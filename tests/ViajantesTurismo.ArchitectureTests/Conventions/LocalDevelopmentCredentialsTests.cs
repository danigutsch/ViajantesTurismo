using System.Text.Json.Nodes;
using SharedKernel.Testing;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Guards the fixed local sign-in passwords: they stay Development-only, name real AppHost
/// parameters, and are documented.
/// </summary>
[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class LocalDevelopmentCredentialsTests
{
    [Fact]
    public void Development_settings_define_only_the_sign_in_passwords()
    {
        // Arrange
        var apphost = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost");
        var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(apphost, "appsettings.Development.json")));

        // Act
        var parameters = settings?["Parameters"]?.AsObject().ToDictionary(pair => pair.Key, pair => pair.Value?.GetValue<string>());

        // Assert
        parameters.ShouldNotBeNull();
        string[] expected =
        [
            ResourceNames.IdentityProviderConformanceUserPassword,
            ResourceNames.IdentityProviderOperatorConformanceUserPassword,
        ];
        parameters.Keys.Order(StringComparer.Ordinal).ToArray().ShouldBe(expected);
        parameters.Values.ShouldNotContain(string.IsNullOrWhiteSpace);
    }

    [Fact]
    public void Non_development_settings_define_no_parameter_values()
    {
        // Arrange
        var apphost = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost");
        var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(apphost, "appsettings.json")));

        // Act
        var parameters = settings?["Parameters"];

        // Assert
        parameters.ShouldBeNull();
    }

    [Fact]
    public void The_readme_documents_each_fixed_parameter_and_value()
    {
        // Arrange
        var apphost = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost");
        var readme = File.ReadAllText(Path.Combine(apphost, "README.md"));
        var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(apphost, "appsettings.Development.json")));

        // Act
        var missing = settings?["Parameters"]?.AsObject()
            .Where(pair => !readme.Contains($"`{pair.Key}`", StringComparison.Ordinal)
                || !readme.Contains($"`{pair.Value?.GetValue<string>()}`", StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .ToArray();

        // Assert
        missing.ShouldNotBeNull();
        missing.ShouldBeEmpty();
    }
}
