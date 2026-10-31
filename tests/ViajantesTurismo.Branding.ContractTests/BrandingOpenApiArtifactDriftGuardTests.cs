using ViajantesTurismo.Branding.ContractTests.Infrastructure;

namespace ViajantesTurismo.Branding.ContractTests;

/// <summary>
/// Verifies that canonical Branding OpenAPI artifacts do not drift from build-generated boundary artifacts.
/// </summary>
public sealed class BrandingOpenApiArtifactDriftGuardTests
{
    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Canonical_branding_OpenApi_artifacts_match_the_generated_boundary_artifacts()
    {
        // Arrange
        var snapshots = BrandingOpenApiArtifactDriftGuard.CreateSnapshotSet();
        IReadOnlyList<string> expectedDrift = [];

        // Act
        var actualDrift = snapshots.GetArtifactDrift();

        // Assert
        actualDrift.ShouldBe(expectedDrift);
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Branding_mutation_keeps_its_published_response_contract()
    {
        // Arrange
        var document = BrandingOpenApiArtifactDriftGuard.CreateSnapshotSet().GetCanonicalSnapshot("branding").AsObject();
        var operation = document["paths"].ShouldNotBeNull().AsObject()
            ["/api/v1/branding/settings"].ShouldNotBeNull().AsObject()
            ["put"].ShouldNotBeNull().AsObject();

        // Act
        var responses = operation["responses"].ShouldNotBeNull().AsObject();
        var responseStatuses = responses.Select(response => response.Key).Order(StringComparer.Ordinal).ToArray();

        // Assert
        responseStatuses.ShouldBe(["200", "400", "401", "403", "412", "428"]);
        responses["200"].ShouldNotBeNull().AsObject().ContainsKey("headers").ShouldBeFalse();
        operation["parameters"].ShouldNotBeNull().AsArray()
            .ShouldContain(parameter => parameter != null && parameter["name"] != null && parameter["name"]?.GetValue<string>() == "If-Match");
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Management_logo_upload_publishes_image_bodies_and_its_response_contract()
    {
        // Arrange
        var document = BrandingOpenApiArtifactDriftGuard.CreateSnapshotSet().GetCanonicalSnapshot("branding").AsObject();
        var operation = document["paths"].ShouldNotBeNull().AsObject()
            ["/api/v1/branding/settings/logo"].ShouldNotBeNull().AsObject()
            ["put"].ShouldNotBeNull().AsObject();

        // Act
        var responseStatuses = operation["responses"].ShouldNotBeNull().AsObject()
            .Select(response => response.Key).Order(StringComparer.Ordinal).ToArray();
        var bodyTypes = operation["requestBody"].ShouldNotBeNull().AsObject()["content"].ShouldNotBeNull().AsObject()
            .Select(content => content.Key).Order(StringComparer.Ordinal).ToArray();

        // Assert
        responseStatuses.ShouldBe(["200", "400", "401", "403", "412", "428", "503"]);
        bodyTypes.ShouldBe(["image/jpeg", "image/png", "image/webp"]);
        operation["parameters"].ShouldNotBeNull().AsArray()
            .ShouldContain(parameter => parameter != null && parameter["name"] != null && parameter["name"]?.GetValue<string>() == "If-Match");
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Public_logo_route_publishes_ok_not_modified_and_not_found()
    {
        // Arrange
        var document = BrandingOpenApiArtifactDriftGuard.CreateSnapshotSet().GetCanonicalSnapshot("public-branding").AsObject();
        var operation = document["paths"].ShouldNotBeNull().AsObject()
            ["/api/v1/public/branding/logo/{sha256}"].ShouldNotBeNull().AsObject()
            ["get"].ShouldNotBeNull().AsObject();

        // Act
        var responseStatuses = operation["responses"].ShouldNotBeNull().AsObject()
            .Select(response => response.Key).Order(StringComparer.Ordinal).ToArray();

        // Assert
        responseStatuses.ShouldBe(["200", "304", "404"]);
    }
}
