using SharedKernel.IntegrationEvents;
using ViajantesTurismo.ArchitectureTests.Infrastructure;
using static ViajantesTurismo.ArchitectureTests.Conventions.NamingConventionTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class NamingConventionTests
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
            .Where(type => type.IsInterface && IsWithinSolution(type.Namespace, SolutionRootNamespaces))
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
            .Where(type => !typeof(IIntegrationEvent).IsAssignableFrom(type))
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

    [Fact]
    public void Xunit_Test_Methods_Should_Follow_Underscore_Naming_Convention()
    {
        var repositoryRoot = GetRepositoryRoot();
        var offendingMethods = Directory
            .GetFiles(Path.Combine(repositoryRoot, "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(path => FindOffendingXunitMethods(repositoryRoot, path))
            .ToArray();

        Assert.False(
            offendingMethods.Length != 0,
            $"Expected xUnit test methods to follow the underscore naming convention, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingMethods)}");
    }

    [Fact]
    public void Behavior_Feature_Files_Should_Use_A_Recognized_Naming_Style()
    {
        var repositoryRoot = GetRepositoryRoot();
        var behaviorSpecsRoot = Path.Combine(
            repositoryRoot,
            "tests",
            "ViajantesTurismo.Admin.BehaviorTests",
            "specs");

        var offendingFiles = Directory
            .GetFiles(behaviorSpecsRoot, "*.feature", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .Where(relativePath =>
            {
                var fileName = Path.GetFileName(relativePath);
                return !KebabCaseFeatureFileRegex().IsMatch(fileName)
                       && !PascalCaseFeatureFileRegex().IsMatch(fileName);
            })
            .ToArray();

        Assert.False(
            offendingFiles.Length != 0,
            $"Expected behavior feature files to use kebab-case or the tracked legacy PascalCase style, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingFiles)}");
    }

    [Fact]
    public void Mediator_Tests_Should_Not_Invoke_Methods_Inside_Simple_Assertions()
    {
        var repositoryRoot = GetRepositoryRoot();
        var mediatorTestsRoot = Path.Combine(repositoryRoot, "tests");
        var offendingAssertions = Directory
            .GetFiles(mediatorTestsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains("SharedKernel.Mediator", StringComparison.Ordinal))
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(path => FindOffendingAssertionMethodCalls(repositoryRoot, path))
            .ToArray();

        Assert.False(
            offendingAssertions.Length != 0,
            $"Expected mediator tests to assign method-call results to locals before simple assertions, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingAssertions)}");
    }

}
