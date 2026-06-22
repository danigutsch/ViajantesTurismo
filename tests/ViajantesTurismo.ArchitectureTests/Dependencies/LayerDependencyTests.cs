using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.xUnitV3;
using ViajantesTurismo.ArchitectureTests.Infrastructure;
using Assembly = System.Reflection.Assembly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ViajantesTurismo.ArchitectureTests.Dependencies;

public sealed class LayerDependencyTests
{
    private static Architecture Architecture => ArchitectureProvider.Architecture;

    private static IObjectProvider<IType> DomainTypes => TypesInNamespace(ArchitectureProvider.Namespaces.Domain, "Domain layer");

    private static IObjectProvider<IType> ApplicationTypes => TypesInNamespace(ArchitectureProvider.Namespaces.Application, "Application layer");

    private static IObjectProvider<IType> InfrastructureTypes => TypesInNamespace(ArchitectureProvider.Namespaces.Infrastructure, "Infrastructure layer");

    private static IObjectProvider<IType> ApiTypes => TypesInNamespace(ArchitectureProvider.Namespaces.Api, "API layer");

    private static IObjectProvider<IType> CatalogDomainTypes => TypesInNamespace(ArchitectureProvider.Namespaces.CatalogDomain, "Catalog domain layer");

    private static IObjectProvider<IType> CatalogApplicationTypes => TypesInNamespace(ArchitectureProvider.Namespaces.CatalogApplication, "Catalog application layer");

    private static IObjectProvider<IType> CatalogInfrastructureTypes => TypesInNamespace(ArchitectureProvider.Namespaces.CatalogInfrastructure, "Catalog infrastructure layer");

    private static IObjectProvider<IType> CatalogApiTypes => TypesInNamespace(ArchitectureProvider.Namespaces.CatalogApi, "Catalog API layer");

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Api()
    {
        var rule = Types().That().Are(DomainTypes)
            .Should().NotDependOnAny(ApplicationTypes)
            .AndShould().NotDependOnAny(InfrastructureTypes)
            .AndShould().NotDependOnAny(ApiTypes)
            .Because("the domain layer must stay persistence and transport agnostic");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var rule = Types().That().Are(ApplicationTypes)
            .Should().NotDependOnAny(ApiTypes)
            .AndShould().NotDependOnAny(InfrastructureTypes)
            .Because("application services orchestrate domain logic and should not reference adapters");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var rule = Types().That().Are(InfrastructureTypes)
            .Should().NotDependOnAny(ApiTypes)
            .Because("infrastructure should expose persistence and integration adapters independently of transport");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Public_Web_Should_Not_Depend_On_Admin_Or_Management_Web()
    {
        // Arrange
        var forbiddenReferences = new[]
        {
            "ViajantesTurismo.Management.Web",
            "ViajantesTurismo.Admin.ApiService",
            "ViajantesTurismo.Admin.Application",
            "ViajantesTurismo.Admin.Contracts",
            "ViajantesTurismo.Admin.Domain",
            "ViajantesTurismo.Admin.Infrastructure"
        };
        var publicWebAssembly = Assembly.Load("ViajantesTurismo.Public.Web");

        // Act
        var referencedAssemblyNames = publicWebAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);
        var unexpectedReferences = forbiddenReferences
            .Where(referencedAssemblyNames.Contains)
            .ToArray();

        // Assert
        Assert.Empty(unexpectedReferences);
    }

    [Fact]
    public void Catalog_Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Api()
    {
        var rule = Types().That().Are(CatalogDomainTypes)
            .Should().NotDependOnAny(CatalogApplicationTypes)
            .AndShould().NotDependOnAny(CatalogInfrastructureTypes)
            .AndShould().NotDependOnAny(CatalogApiTypes)
            .Because("the Catalog domain layer must stay persistence and transport agnostic");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Catalog_Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var rule = Types().That().Are(CatalogApplicationTypes)
            .Should().NotDependOnAny(CatalogApiTypes)
            .AndShould().NotDependOnAny(CatalogInfrastructureTypes)
            .Because("Catalog application services should not reference adapters");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Catalog_Infrastructure_Should_Not_Depend_On_Api()
    {
        var rule = Types().That().Are(CatalogInfrastructureTypes)
            .Should().NotDependOnAny(CatalogApiTypes)
            .Because("Catalog infrastructure should remain independent of transport");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Catalog_Should_Not_Depend_On_Admin_Implementation_Projects()
    {
        // Arrange
        var forbiddenReferences = new[]
        {
            "ViajantesTurismo.Admin.ApiService",
            "ViajantesTurismo.Admin.Application",
            "ViajantesTurismo.Admin.Domain",
            "ViajantesTurismo.Admin.Infrastructure"
        };
        var catalogAssemblies = ArchitectureProvider.Assemblies
            .Where(assembly => assembly.GetName().Name?.StartsWith("ViajantesTurismo.Catalog.", StringComparison.Ordinal) == true)
            .ToArray();

        // Act
        var unexpectedReferences = catalogAssemblies
            .SelectMany(assembly => assembly.GetReferencedAssemblies()
                .Select(reference => new { CatalogAssembly = assembly.GetName().Name, ReferencedAssembly = reference.Name }))
            .Where(reference => forbiddenReferences.Contains(reference.ReferencedAssembly, StringComparer.Ordinal))
            .Select(reference => $"{reference.CatalogAssembly} -> {reference.ReferencedAssembly}")
            .ToArray();

        // Assert
        Assert.Empty(unexpectedReferences);
    }

    private static GivenTypesConjunctionWithDescription TypesInNamespace(string namespaceRoot, string description)
    {
        var pattern = $"^{Regex.Escape(namespaceRoot)}(\\.|$)";
        return Types().That().ResideInNamespaceMatching(pattern).As(description);
    }
}
