using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.xUnitV3;
using ViajantesTurismo.ArchitectureTests.Infrastructure;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ViajantesTurismo.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static Architecture Architecture => ArchitectureProvider.Architecture;

    private static IObjectProvider<IType> DomainTypes => TypesInNamespace(ArchitectureProvider.Namespaces.Domain, "Domain layer");

    private static IObjectProvider<IType> ApplicationTypes => TypesInNamespace(ArchitectureProvider.Namespaces.Application, "Application layer");

    private static IObjectProvider<IType> InfrastructureTypes => TypesInNamespace(ArchitectureProvider.Namespaces.Infrastructure, "Infrastructure layer");

    private static IObjectProvider<IType> ApiTypes => TypesInNamespace(ArchitectureProvider.Namespaces.Api, "API layer");

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Api()
    {
        Types().That().Are(DomainTypes)
            .Should().NotDependOnAny(ApplicationTypes)
            .AndShould().NotDependOnAny(InfrastructureTypes)
            .AndShould().NotDependOnAny(ApiTypes)
            .Because("the domain layer must stay persistence and transport agnostic")
            .Check(Architecture);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        Types().That().Are(ApplicationTypes)
            .Should().NotDependOnAny(ApiTypes)
            .AndShould().NotDependOnAny(InfrastructureTypes)
            .Because("application services orchestrate domain logic and should not reference adapters")
            .Check(Architecture);
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        Types().That().Are(InfrastructureTypes)
            .Should().NotDependOnAny(ApiTypes)
            .Because("infrastructure should expose persistence and integration adapters independently of transport")
            .Check(Architecture);
    }

    private static GivenTypesConjunctionWithDescription TypesInNamespace(string namespaceRoot, string description)
    {
        var pattern = $"^{Regex.Escape(namespaceRoot)}(\\.|$)";
        return Types().That().ResideInNamespaceMatching(pattern).As(description);
    }
}