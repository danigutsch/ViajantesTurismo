using PandaBear.Testing;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Protects the mapping of API resource locations to UI routes.</summary>
[TestScope(TestScope.Unit)]
public sealed class ApiLocationNormalizerTests
{
    [Theory]
    [InlineData("https://api.example.test/api/v1/tours/42", "/tours/42")]
    [InlineData("/api/v1/tours/42", "/tours/42")]
    [InlineData("api/v1/tours/42", "/tours/42")]
    [InlineData("/api/v1/tours", "/tours")]
    [InlineData("/api/v1/toursextra/42", "/api/v1/toursextra/42")]
    [InlineData("tours/42", "/tours/42")]
    public void Locations_become_local_routes_without_the_API_version_prefix(string location, string expectedRoute)
    {
        // Arrange
        var uri = new Uri(location, UriKind.RelativeOrAbsolute);

        // Act
        var route = ApiLocationNormalizer.ToUiRoute(uri, "/fallback", "/api/v1/tours");

        // Assert
        route.ShouldBe(expectedRoute);
    }

    [Fact]
    public void A_missing_location_falls_back_to_the_given_path()
    {
        // Act
        var route = ApiLocationNormalizer.ToUiRoute(null, "/fallback", "/api/v1/tours");

        // Assert
        route.ShouldBe("/fallback");
    }

    [Fact]
    public void A_protocol_relative_location_falls_back_to_the_given_path()
    {
        // Arrange
        var uri = new Uri("//attacker.example/api/v1/tours/42", UriKind.Relative);

        // Act
        var route = ApiLocationNormalizer.ToUiRoute(uri, "/fallback", "/api/v1/tours");

        // Assert
        route.ShouldBe("/fallback");
    }

    [Fact]
    public void A_resource_prefix_outside_the_API_version_is_rejected()
    {
        // Act
        var action = () => ApiLocationNormalizer.ToUiRoute(null, "/fallback", "/tours");

        // Assert
        action.ShouldThrow<ArgumentException>();
    }
}
