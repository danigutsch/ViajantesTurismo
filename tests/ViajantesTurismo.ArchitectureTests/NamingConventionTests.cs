using ViajantesTurismo.ArchitectureTests.Infrastructure;

namespace ViajantesTurismo.ArchitectureTests;

public sealed class NamingConventionTests
{
    private static readonly string[] SolutionRootNamespaces =
    [
        ArchitectureProvider.Namespaces.Domain,
        ArchitectureProvider.Namespaces.Application,
        ArchitectureProvider.Namespaces.Infrastructure,
        ArchitectureProvider.Namespaces.Api,
        ArchitectureProvider.Namespaces.Contracts,
        ArchitectureProvider.Namespaces.Common
    ];

    [Fact]
    public void Interfaces_Should_Start_With_I()
    {
        var offendingTypes = ArchitectureProvider.Assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsInterface && IsWithinSolution(type.Namespace))
            .Where(type => !type.Name.StartsWith('I'))
            .ToArray();

        Assert.False(
            offendingTypes.Length != 0,
            $"Expected all interfaces to start with 'I', but found: {string.Join(", ", offendingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void ContractDtos_Should_End_With_Dto()
    {
        const string contractNamespace = ArchitectureProvider.Namespaces.Contracts;
        var offendingTypes = ArchitectureProvider.Assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.Namespace is not null && type.Namespace.StartsWith(contractNamespace, StringComparison.Ordinal))
            .Where(type => type.IsClass)
            .Where(type => !IsStaticClass(type))
            .Where(type => !typeof(Attribute).IsAssignableFrom(type))
            .Where(type => !type.Name.EndsWith("Dto", StringComparison.Ordinal))
            .ToArray();

        Assert.False(
            offendingTypes.Length != 0,
            $"Expected contract types to end with 'Dto', but found: {string.Join(", ", offendingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void TestClasses_Should_End_With_Tests()
    {
        var testsAssembly = typeof(NamingConventionTests).Assembly;
        var offendingTypes = testsAssembly.GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => !type.Name.EndsWith("Tests", StringComparison.Ordinal))
            .ToArray();

        Assert.False(
            offendingTypes.Length != 0,
            $"Expected architecture test classes to end with 'Tests', but found: {string.Join(", ", offendingTypes.Select(t => t.FullName))}");
    }

    private static bool IsWithinSolution(string? @namespace)
    {
        if (@namespace is null)
        {
            return false;
        }

        return SolutionRootNamespaces.Any(root => @namespace.StartsWith(root, StringComparison.Ordinal));
    }

    private static bool IsStaticClass(Type type) => type is { IsAbstract: true, IsSealed: true };
}