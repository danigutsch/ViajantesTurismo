using System.Text.RegularExpressions;
using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AdminTestArchitectureGuardTests
{
    [Fact]
    public void Admin_test_architecture_guide_should_declare_the_canonical_documentation_owner()
    {
        var repositoryRoot = GetRepositoryRoot();
        var guidePath = Path.Combine(repositoryRoot, "tests", "README.md");
        var guideText = File.ReadAllText(guidePath);
        var architectureReadmePath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.ArchitectureTests", "README.md");
        var architectureReadmeText = File.ReadAllText(architectureReadmePath);
        var uiIntegrationReadmePath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.UiIntegrationTests", "README.md");
        var uiIntegrationReadmeText = File.ReadAllText(uiIntegrationReadmePath);
        var uiIntegrationScaffoldTestPath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.UiIntegrationTests", "ScaffoldTests.cs");
        var uiIntegrationScaffoldTestText = File.ReadAllText(uiIntegrationScaffoldTestPath);

        Assert.Contains("This file is the canonical quick-reference for the Admin test taxonomy.", guideText, StringComparison.Ordinal);
        Assert.Contains("docs/TEST_GUIDELINES.md", guideText, StringComparison.Ordinal);
        Assert.Contains("AdminTestArchitectureGuardTests", architectureReadmeText, StringComparison.Ordinal);
        Assert.Contains("tests/README.md", architectureReadmeText, StringComparison.Ordinal);
        Assert.Contains("Keep it scaffold-only until a concrete Admin route-composition scenario clearly belongs here", uiIntegrationReadmeText, StringComparison.Ordinal);
        Assert.Contains("Project_Remains_A_Scaffold_Until_A_Real_Admin_UI_Integration_Slice_Exists", uiIntegrationScaffoldTestText, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_hosted_test_infrastructure_should_use_the_approved_fixture_and_base_class_model()
    {
        var repositoryRoot = GetRepositoryRoot();
        var integrationInfrastructurePath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.IntegrationTests", "Infrastructure");
        var systemTestBasesPath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.SystemTests", "Infrastructure", "Bases");
        var systemTestFixturesPath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.SystemTests", "Infrastructure", "Fixtures");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "AssemblyFixture.cs"),
            "[assembly: Xunit.AssemblyFixture(typeof(ViajantesTurismo.Admin.IntegrationTests.Infrastructure.ApiFixture))]");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "ApiFixture.cs"),
            "public sealed class ApiFixture : ViajantesTurismo.Admin.Testing.Integration.IAdminTestHost, IAsyncLifetime");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "ApiFixture.cs"),
            "_app = await AspireTestApplication.Start<ViajantesTurismo_AppHost>([ResourceNames.Api], null, TestContext.Current.CancellationToken);");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "ApiFixture.cs"),
            "_client = _app.CreateHttpClient(ResourceNames.Api);");

        AssertFileContains(
            Path.Combine(GetRepositoryRoot(), "tests", "SharedKernel.IntegrationTesting", "PostgreSqlPublicSchemaReset.cs"),
            "public static async Task Reset(DbConnection connection, CancellationToken ct)");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "Fixtures", "AspireSerialIntegrationTestFixture.cs"),
            "public sealed class AspireSerialIntegrationTestFixture : IAsyncLifetime, IDisposable");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "Fixtures", "AspireSerialIntegrationTestCollection.cs"),
            "[CollectionDefinition(IntegrationTestCollections.Serial, DisableParallelization = true)]");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "Fixtures", "AspireSerialIntegrationTestFixture.cs"),
            "await PostgreSqlPublicSchemaReset.Reset(connection, ct);");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "Bases", "AspireSerialIntegrationTestBase.cs"),
            "public abstract class AspireSerialIntegrationTestBase(");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "Bases", "AspireSerialIntegrationTestBase.cs"),
            "AspireSerialIntegrationTestFixture fixture) : IAsyncLifetime");

        AssertFileContains(
            Path.Combine(integrationInfrastructurePath, "Bases", "AspireSerialIntegrationTestBase.cs"),
            "await fixture.ResetToKnownBaseline(cts.Token);");

        AssertFileDoesNotContain(
            Path.Combine(integrationInfrastructurePath, "Bases", "AspireSerialIntegrationTestBase.cs"),
            new Regex(@"public\s+virtual\s+async\s+ValueTask\s+DisposeAsync\s*\(\s*\)\s*\{[^}]*ResetDatabase\(", RegexOptions.Singleline | RegexOptions.CultureInvariant));

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSystemTestBase.cs"),
            "public abstract class AspireSystemTestBase<TFixture>(TFixture fixture) : PageTest");

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSystemTestBase.cs"),
            "protected Uri ApiBaseUri => Fixture.ApiBaseUri;");

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSerialSystemTestBase.cs"),
            "[Collection(E2ETestCollections.Serial)]");

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSerialSystemTestBase.cs"),
            "public abstract class AspireSerialSystemTestBase(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)");

        AssertFileContains(
            Path.Combine(systemTestFixturesPath, "AspireSystemTestFixture.cs"),
            "await PostgreSqlPublicSchemaReset.Reset(connection, ct);");

        AssertFileContains(
            Path.Combine(systemTestBasesPath, "AspireSerialSystemTestBase.cs"),
            "await Fixture.ResetToKnownBaseline(cts.Token);");

        AssertFileDoesNotContain(
            Path.Combine(systemTestBasesPath, "AspireSerialSystemTestBase.cs"),
            ProtectedClearDatabaseMemberRegex());

        AssertFileContains(
            Path.Combine(systemTestFixturesPath, "AspireSystemTestFixture.cs"),
            "public sealed class AspireSystemTestFixture : IAspireSystemTestFixture, IAsyncLifetime, IDisposable");

        AssertFileDoesNotExist(
            Path.Combine(systemTestFixturesPath, "AspireSerialSystemTestFixture.cs"));

        AssertFileDoesNotExist(
            Path.Combine(systemTestBasesPath, "E2ETestBase.cs"));

        AssertFileDoesNotExist(
            Path.Combine(systemTestBasesPath, "E2ESerialTestBase.cs"));

        AssertFileDoesNotExist(
            Path.Combine(systemTestFixturesPath, "E2EFixture.cs"));
    }

    [Fact]
    public void SystemTests_should_keep_serial_collection_control_in_base_classes_only()
    {
        var systemTestsPath = Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.Admin.SystemTests");

        var violatingFiles = Directory.GetFiles(systemTestsPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .Where(path => !path.Contains("/Infrastructure/", StringComparison.Ordinal)
                && !path.Contains("\\Infrastructure\\", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("[Collection(E2ETestCollections.Serial)]", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(GetRepositoryRoot(), path).Replace('\\', '/'))
            .ToArray();

        Assert.True(
            violatingFiles.Length == 0,
            $"Expected serial collection ownership to stay in base-class infrastructure, but found direct usage in:{Environment.NewLine}{string.Join(Environment.NewLine, violatingFiles)}");
    }

    [Fact]
    public void SystemTests_should_document_each_serial_test_with_a_reason()
    {
        var systemTestsPath = Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.Admin.SystemTests");

        var undocumentedSerialTests = Directory.GetFiles(systemTestsPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .Where(path => !path.Contains("/Infrastructure/", StringComparison.Ordinal)
                && !path.Contains("\\Infrastructure\\", StringComparison.Ordinal))
            .SelectMany(FindUndocumentedSerialTests)
            .ToArray();

        Assert.True(
            undocumentedSerialTests.Length == 0,
            $"Expected each serial E2E test to declare [SerialE2EReason], but found:{Environment.NewLine}{string.Join(Environment.NewLine, undocumentedSerialTests)}");
    }

    [Fact]
    public void Serial_test_collection_definitions_should_declare_a_justification()
    {
        var testsRoot = Path.Combine(GetRepositoryRoot(), "tests");

        var undocumentedSerialDefinitions = Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .Where(path => !path.Contains("/SharedKernel.Testing.Analyzers.Tests/", StringComparison.Ordinal)
                && !path.Contains("\\SharedKernel.Testing.Analyzers.Tests\\", StringComparison.Ordinal))
            .SelectMany(FindUndocumentedSerialCollectionDefinitions)
            .ToArray();

        Assert.True(
            undocumentedSerialDefinitions.Length == 0,
            $"Expected every DisableParallelization = true collection definition to include a nearby [SerialTestJustification] attribute, but found:{Environment.NewLine}{string.Join(Environment.NewLine, undocumentedSerialDefinitions)}");
    }

    [Fact]
    public void Admin_hosted_test_infrastructure_should_not_expose_generic_serviceProvider_reach_through()
    {
        var infrastructureRoots = new[]
        {
            Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.Admin.IntegrationTests", "Infrastructure"),
            Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.Admin.SystemTests", "Infrastructure")
        };

        var offendingMembers = infrastructureRoots
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(path => FindGenericServiceProviderReachThrough(path))
            .ToArray();

        Assert.False(
            offendingMembers.Length != 0,
            $"Expected Admin hosted test infrastructure to avoid public generic service-provider reach-through, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingMembers)}");
    }

    [Fact]
    public void Concrete_test_methods_should_not_own_raw_serviceProvider_or_scope_plumbing()
    {
        var testsRoot = Path.Combine(GetRepositoryRoot(), "tests");
        var offendingLines = Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(FindRawServiceProviderPlumbingInTestMethods)
            .ToArray();

        Assert.False(
            offendingLines.Length != 0,
            $"Expected concrete test methods to use typed helpers instead of raw DI/scope plumbing, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingLines)}");
    }

    [Fact]
    public void Test_trait_names_should_use_canonical_constants()
    {
        var testsRoot = Path.Combine(GetRepositoryRoot(), "tests");
        var offendingLines = Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(FindHardcodedCanonicalTraitNames)
            .ToArray();

        Assert.False(
            offendingLines.Length != 0,
            $"Expected reusable trait names to come from canonical constants directly, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingLines)}");
    }

    [Fact]
    public void SharedKernel_testing_should_not_own_product_or_area_specific_trait_values()
    {
        var sharedKernelTestingRoot = Path.Combine(GetRepositoryRoot(), "tests", "SharedKernel.Testing");
        var offendingLines = Directory.GetFiles(sharedKernelTestingRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(FindProductSpecificSharedKernelTestingCoupling)
            .ToArray();

        Assert.False(
            offendingLines.Length != 0,
            $"Expected SharedKernel.Testing to stay neutral and leave product or area-specific trait values local, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingLines)}");
    }

}
