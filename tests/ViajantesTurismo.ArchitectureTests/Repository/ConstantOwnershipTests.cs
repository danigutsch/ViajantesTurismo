using SharedKernel.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

[TestFocus(TestFocus.DriftGuard)]
public sealed class ConstantOwnershipTests
{
    [Theory]
    [InlineData("ViajantesTurismo.Admin.Api/Program.cs", "\"User-agent: *\\nDisallow: /\"")]
    [InlineData("ViajantesTurismo.Catalog.Api/Program.cs", "\"User-agent: *\\nDisallow: /\"")]
    [InlineData("ViajantesTurismo.Catalog.Api/PublicContent/PublicContentHttpCache.cs", "\"culture\"")]
    [InlineData("ViajantesTurismo.Catalog.Api/PublicContent/PublicContentHttpCache.cs", "\"language\"")]
    [InlineData("ViajantesTurismo.Public.Web/PublicCultureRedirects.cs", "\"culture\"")]
    [InlineData("ViajantesTurismo.Public.Web/PublicCultureRedirects.cs", "\"language\"")]
    [InlineData("ViajantesTurismo.Catalog.Contracts.Http/PublicCatalogApiClient.cs", "?culture={")]
    [InlineData("ViajantesTurismo.AppHost/AppHostResourceExtensions.cs", "const string KeycloakTokenExchangeProvider")]
    [InlineData("ViajantesTurismo.Management.Web/ManagementAuthenticationDefaults.cs", "\"Keycloak\"")]
    [InlineData("ViajantesTurismo.Admin.Api/Documents/DocumentEndpoints.cs", "\"text/html; charset=utf-8\"")]
    [InlineData("ViajantesTurismo.Management.Web/DocumentArtifactProxyEndpoints.cs", "\"text/html; charset=utf-8\"")]
    [InlineData("ViajantesTurismo.Architecture.Analyzers/ProductionApiHttpAnalyzer.cs", "SharedKernelBrandingNamespace")]
    public void Consumers_use_the_canonical_application_policy_constants(string sourcePath, string localDeclaration)
    {
        // Arrange
        var file = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", sourcePath);

        // Act
        var source = File.ReadAllText(file);

        // Assert
        source.ShouldNotContain(localDeclaration, StringComparison.Ordinal);
    }
}
