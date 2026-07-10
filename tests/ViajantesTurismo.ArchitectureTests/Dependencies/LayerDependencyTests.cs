using ArchUnitNET.Domain;
using ArchUnitNET.xUnitV3;
using ViajantesTurismo.ArchitectureTests.Infrastructure;
using Assembly = System.Reflection.Assembly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ViajantesTurismo.ArchitectureTests.Dependencies.LayerDependencyTestsHelpers;

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
    public void Domain_should_not_depend_on_application_infrastructure_or_api()
    {
        var rule = Types().That().Are(DomainTypes)
            .Should().NotDependOnAny(ApplicationTypes)
            .AndShould().NotDependOnAny(InfrastructureTypes)
            .AndShould().NotDependOnAny(ApiTypes)
            .Because("the domain layer must stay persistence and transport agnostic");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        var rule = Types().That().Are(ApplicationTypes)
            .Should().NotDependOnAny(ApiTypes)
            .AndShould().NotDependOnAny(InfrastructureTypes)
            .Because("application services orchestrate domain logic and should not reference adapters");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_api()
    {
        var rule = Types().That().Are(InfrastructureTypes)
            .Should().NotDependOnAny(ApiTypes)
            .Because("infrastructure should expose persistence and integration adapters independently of transport");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Public_web_should_not_depend_on_admin_or_management_web()
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
        unexpectedReferences.ShouldBeEmpty();
    }

    [Fact]
    public void Catalog_domain_should_not_depend_on_application_infrastructure_or_api()
    {
        var rule = Types().That().Are(CatalogDomainTypes)
            .Should().NotDependOnAny(CatalogApplicationTypes)
            .AndShould().NotDependOnAny(CatalogInfrastructureTypes)
            .AndShould().NotDependOnAny(CatalogApiTypes)
            .Because("the Catalog domain layer must stay persistence and transport agnostic");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Catalog_application_should_not_depend_on_infrastructure_or_api()
    {
        var rule = Types().That().Are(CatalogApplicationTypes)
            .Should().NotDependOnAny(CatalogApiTypes)
            .AndShould().NotDependOnAny(CatalogInfrastructureTypes)
            .Because("Catalog application services should not reference adapters");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Catalog_infrastructure_should_not_depend_on_api()
    {
        var rule = Types().That().Are(CatalogInfrastructureTypes)
            .Should().NotDependOnAny(CatalogApiTypes)
            .Because("Catalog infrastructure should remain independent of transport");

        ArchRuleAssert.CheckRule(Architecture, rule);
    }

    [Fact]
    public void Catalog_should_not_depend_on_admin_implementation_projects()
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
        unexpectedReferences.ShouldBeEmpty();
    }

    [Fact]
    public void SharedKernel_projects_should_not_reference_product_projects()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var unexpectedReferences = FindSharedKernelProductReferences(repositoryRoot);

        // Assert
        unexpectedReferences.ShouldBeEmpty();
    }

    [Fact]
    public void Domain_application_and_contract_projects_should_not_reference_adapter_packages()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var unexpectedReferences = FindLayerAdapterPackageReferences(repositoryRoot);

        // Assert
        unexpectedReferences.ShouldBeEmpty();
    }

    [Fact]
    public void Provider_neutral_sharedkernel_projects_should_not_reference_adapter_packages()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var unexpectedReferences = FindProviderNeutralSharedKernelAdapterPackageReferences(repositoryRoot);

        // Assert
        unexpectedReferences.ShouldBeEmpty();
    }

    [Fact]
    public void SharedKernel_entityframeworkcore_adapter_projects_should_name_entityframeworkcore()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var unexpectedReferences = FindSharedKernelEntityFrameworkCoreAdapterNamingViolations(repositoryRoot);

        // Assert
        unexpectedReferences.ShouldBeEmpty();
    }

    [Fact]
    public void SharedKernel_project_names_should_not_use_core_as_a_module_segment()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var unexpectedProjects = FindSharedKernelCoreSegmentProjectViolations(repositoryRoot);

        // Assert
        unexpectedProjects.ShouldBeEmpty();
    }

    [Fact]
    public void SharedKernel_primary_modules_should_not_reference_descendant_optional_submodules_at_runtime()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var unexpectedReferences = FindSharedKernelRuntimeReferencesToDescendantOptionalSubmodules(repositoryRoot);

        // Assert
        unexpectedReferences.ShouldBeEmpty();
    }

    [Fact]
    public void Abstraction_projects_should_not_reference_implementation_projects_or_adapter_packages()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var unexpectedReferences = FindAbstractionProjectImplementationReferences(repositoryRoot);

        // Assert
        unexpectedReferences.ShouldBeEmpty();
    }

    [Fact]
    public void Module_boundary_documentation_should_describe_enforced_rules()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var missingRules = FindModuleBoundaryDocumentationRuleGaps(repositoryRoot);

        // Assert
        missingRules.ShouldBeEmpty();
    }

}
