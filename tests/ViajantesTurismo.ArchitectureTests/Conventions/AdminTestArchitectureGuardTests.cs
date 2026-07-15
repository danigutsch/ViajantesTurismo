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

        guideText.ShouldContain("This file is the canonical quick-reference for the Admin test taxonomy.", StringComparison.Ordinal);
        guideText.ShouldContain("docs/TEST_GUIDELINES.md", StringComparison.Ordinal);
        architectureReadmeText.ShouldContain("AdminTestArchitectureGuardTests", StringComparison.Ordinal);
        architectureReadmeText.ShouldContain("tests/README.md", StringComparison.Ordinal);
        uiIntegrationReadmeText.ShouldContain("Keep it scaffold-only until a concrete Admin route-composition scenario clearly belongs here", StringComparison.Ordinal);
        uiIntegrationScaffoldTestText.ShouldContain("Project_remains_a_scaffold_until_a_real_admin_UI_integration_slice_exists", StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_hosted_test_infrastructure_should_use_the_approved_fixture_and_base_class_model()
    {
        var repositoryRoot = GetRepositoryRoot();
        var integrationInfrastructurePath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.IntegrationTests", "Infrastructure");
        var systemTestBasesPath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.SystemTests", "Infrastructure", "Bases");
        var systemTestFixturesPath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.Admin.SystemTests", "Infrastructure", "Fixtures");
        var assemblyFixtureText = File.ReadAllText(Path.Combine(integrationInfrastructurePath, "AssemblyFixture.cs"));
        var apiFixtureText = File.ReadAllText(Path.Combine(integrationInfrastructurePath, "ApiFixture.cs"));
        var publicSchemaResetText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "SharedKernel",
            "SharedKernel.IntegrationTesting",
            "PostgreSqlPublicSchemaReset.cs"));
        var serialIntegrationCollectionText = File.ReadAllText(Path.Combine(
            integrationInfrastructurePath,
            "Fixtures",
            "AspireSerialIntegrationTestCollection.cs"));
        var serialIntegrationBaseText = File.ReadAllText(Path.Combine(
            integrationInfrastructurePath,
            "Bases",
            "AspireSerialIntegrationTestBase.cs"));
        var systemTestBaseText = File.ReadAllText(Path.Combine(systemTestBasesPath, "AspireSystemTestBase.cs"));
        var serialSystemTestBaseText = File.ReadAllText(Path.Combine(systemTestBasesPath, "AspireSerialSystemTestBase.cs"));
        var systemFixtureText = File.ReadAllText(Path.Combine(systemTestFixturesPath, "AspireSystemTestFixture.cs"));
        var obsoleteIntegrationFixtureExists = File.Exists(Path.Combine(
            integrationInfrastructurePath,
            "Fixtures",
            "AspireSerialIntegrationTestFixture.cs"));
        var obsoleteSystemFixtureExists = File.Exists(Path.Combine(systemTestFixturesPath, "AspireSerialSystemTestFixture.cs"));
        var obsoleteE2ETestBaseExists = File.Exists(Path.Combine(systemTestBasesPath, "E2ETestBase.cs"));
        var obsoleteE2ESerialTestBaseExists = File.Exists(Path.Combine(systemTestBasesPath, "E2ESerialTestBase.cs"));
        var obsoleteE2EFixtureExists = File.Exists(Path.Combine(systemTestFixturesPath, "E2EFixture.cs"));

        assemblyFixtureText.ShouldContain(
            "[assembly: Xunit.AssemblyFixture(typeof(ViajantesTurismo.Admin.IntegrationTests.Infrastructure.ApiFixture))]",
            StringComparison.Ordinal);
        apiFixtureText.ShouldContain(
            "public sealed class ApiFixture : Testing.Integration.IAdminTestHost, IAsyncLifetime",
            StringComparison.Ordinal);
        apiFixtureText.ShouldContain("var testConfiguration = AppHostTestArguments.CreateConfiguration();", StringComparison.Ordinal);
        apiFixtureText.ShouldContain("HostedProfile.Admin.ToArguments()", StringComparison.Ordinal);
        apiFixtureText.ShouldContain("string[] appHostArguments =", StringComparison.Ordinal);
        apiFixtureText.ShouldContain("_app = await AspireTestApplication.Start<ViajantesTurismo_AppHost>(", StringComparison.Ordinal);
        apiFixtureText.ShouldContain("_client = _app.CreateHttpClient(ResourceNames.Api);", StringComparison.Ordinal);
        apiFixtureText.ShouldContain(
            "_databaseConnectionString = await _app.GetConnectionString(ResourceNames.AdminDatabase, TestContext.Current.CancellationToken);",
            StringComparison.Ordinal);
        publicSchemaResetText.ShouldContain(
            "public static async Task Reset(DbConnection connection, CancellationToken ct)",
            StringComparison.Ordinal);
        apiFixtureText.ShouldContain("await PostgreSqlPublicSchemaReset.Reset(connection, ct);", StringComparison.Ordinal);
        serialIntegrationCollectionText.ShouldContain(
            "[CollectionDefinition(IntegrationTestCollections.Serial, DisableParallelization = true)]",
            StringComparison.Ordinal);

        serialIntegrationCollectionText.ShouldNotMatch(new Regex("ICollectionFixture<", RegexOptions.CultureInvariant));

        serialIntegrationBaseText.ShouldContain("public abstract class AspireSerialIntegrationTestBase(", StringComparison.Ordinal);
        serialIntegrationBaseText.ShouldContain("ApiFixture fixture) : IAsyncLifetime", StringComparison.Ordinal);
        serialIntegrationBaseText.ShouldContain("await fixture.ResetToKnownBaseline(cts.Token);", StringComparison.Ordinal);

        serialIntegrationBaseText.ShouldNotMatch(
            new Regex(
                @"public\s+virtual\s+async\s+ValueTask\s+DisposeAsync\s*\(\s*\)\s*\{[^}]*ResetDatabase\(",
                RegexOptions.Singleline | RegexOptions.CultureInvariant));

        obsoleteIntegrationFixtureExists.ShouldBeFalse();

        systemTestBaseText.ShouldContain(
            "public abstract class AspireSystemTestBase<TFixture>(TFixture fixture) : PageTest",
            StringComparison.Ordinal);
        systemTestBaseText.ShouldContain("protected Uri ApiBaseUri => Fixture.ApiBaseUri;", StringComparison.Ordinal);
        serialSystemTestBaseText.ShouldContain("[Collection(E2ETestCollections.Serial)]", StringComparison.Ordinal);
        serialSystemTestBaseText.ShouldContain(
            "public abstract class AspireSerialSystemTestBase(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)",
            StringComparison.Ordinal);
        systemFixtureText.ShouldContain("await PostgreSqlPublicSchemaReset.Reset(connection, ct);", StringComparison.Ordinal);
        serialSystemTestBaseText.ShouldContain("await Fixture.ResetToKnownBaseline(cts.Token);", StringComparison.Ordinal);

        serialSystemTestBaseText.ShouldNotMatch(ProtectedClearDatabaseMemberRegex());

        systemFixtureText.ShouldContain(
            "public sealed class AspireSystemTestFixture : IAspireSystemTestFixture, IAsyncLifetime, IDisposable",
            StringComparison.Ordinal);

        obsoleteSystemFixtureExists.ShouldBeFalse();
        obsoleteE2ETestBaseExists.ShouldBeFalse();
        obsoleteE2ESerialTestBaseExists.ShouldBeFalse();
        obsoleteE2EFixtureExists.ShouldBeFalse();
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

        (violatingFiles.Length == 0).ShouldBeTrue(
            $"Expected serial collection ownership to stay in base-class infrastructure, but found direct usage in:{Environment.NewLine}{string.Join(Environment.NewLine, violatingFiles)}");
    }

    [Fact]
    public void Management_web_should_render_routes_only_after_interactive_server_connection()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();
        var componentsPath = Path.Combine(repositoryRoot, "src", "ViajantesTurismo.Management.Web", "Components");
        var pagesPath = Path.Combine(componentsPath, "Pages");
        var appPath = Path.Combine(componentsPath, "App.razor");
        var mainLayoutPath = Path.Combine(componentsPath, "Layout", "MainLayout.razor");
        var appStylesPath = Path.Combine(repositoryRoot, "src", "ViajantesTurismo.Management.Web", "wwwroot", "app.css");
        var noScriptStylesPath = Path.Combine(repositoryRoot, "src", "ViajantesTurismo.Management.Web", "wwwroot", "app-noscript.css");
        var interactiveReadyPath = Path.Combine(componentsPath, "InteractiveReady.razor");

        // Act
        var appMarkup = File.ReadAllText(appPath);
        var mainLayoutMarkup = File.ReadAllText(mainLayoutPath);
        var appStyles = File.ReadAllText(appStylesPath);
        var noScriptStylesExists = File.Exists(noScriptStylesPath);
        var noScriptStyles = noScriptStylesExists ? File.ReadAllText(noScriptStylesPath) : string.Empty;
        var interactiveReadyExists = File.Exists(interactiveReadyPath);
        var pagesWithRouteRenderModes = Directory.GetFiles(pagesPath, "*.razor", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("@rendermode", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();

        // Assert
        (appMarkup).ShouldContain("<HeadOutlet @rendermode=\"InteractiveServer\" />", StringComparison.Ordinal);
        (appMarkup).ShouldContain("<Routes @rendermode=\"InteractiveServer\" />", StringComparison.Ordinal);
        (appMarkup).ShouldContain("<div class=\"app-startup-status\" role=\"status\" aria-live=\"polite\">", StringComparison.Ordinal);
        (appMarkup).ShouldContain("<noscript>", StringComparison.Ordinal);
        (appMarkup).ShouldContain("<link rel=\"stylesheet\" href=\"@Assets[\"app-noscript.css\"]\" />", StringComparison.Ordinal);
        (mainLayoutMarkup).ShouldContain("inert=\"@(!RendererInfo.IsInteractive ? \"inert\" : null)\"", StringComparison.Ordinal);
        (mainLayoutMarkup).ShouldContain("aria-busy=\"@(!RendererInfo.IsInteractive ? \"true\" : \"false\")\"", StringComparison.Ordinal);
        (appStyles).ShouldContain("body:has(.page:not([inert])) .app-startup-status", StringComparison.Ordinal);
        noScriptStylesExists.ShouldBeTrue();
        (noScriptStyles).ShouldContain(".app-startup-status { display: none; }", StringComparison.Ordinal);
        interactiveReadyExists.ShouldBeFalse();
        pagesWithRouteRenderModes.ShouldBeEmpty();
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

        (undocumentedSerialTests.Length == 0).ShouldBeTrue(
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

        (undocumentedSerialDefinitions.Length == 0).ShouldBeTrue(
            $"Expected every DisableParallelization = true collection definition to include a nearby [SerialTestJustification] attribute, but found:{Environment.NewLine}{string.Join(Environment.NewLine, undocumentedSerialDefinitions)}");
    }

    [Fact]
    public void Admin_hosted_test_infrastructure_should_not_expose_generic_serviceprovider_reach_through()
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

        (offendingMembers.Length != 0).ShouldBeFalse(
            $"Expected Admin hosted test infrastructure to avoid public generic service-provider reach-through, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingMembers)}");
    }

    [Fact]
    public void Concrete_test_methods_should_not_own_raw_serviceprovider_or_scope_plumbing()
    {
        var testsRoot = Path.Combine(GetRepositoryRoot(), "tests");
        var offendingLines = Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(FindRawServiceProviderPlumbingInTestMethods)
            .ToArray();

        (offendingLines.Length != 0).ShouldBeFalse(
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

        (offendingLines.Length != 0).ShouldBeFalse(
            $"Expected reusable trait names to come from canonical constants directly, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingLines)}");
    }

    [Fact]
    public void SharedKernel_testing_should_not_own_product_or_area_specific_trait_values()
    {
        var sharedKernelTestingRoot = Path.Combine(GetRepositoryRoot(), "src", "SharedKernel", "SharedKernel.Testing");
        var offendingLines = Directory.GetFiles(sharedKernelTestingRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(FindProductSpecificSharedKernelTestingCoupling)
            .ToArray();

        (offendingLines.Length != 0).ShouldBeFalse(
            $"Expected SharedKernel.Testing to stay neutral and leave product or area-specific trait values local, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingLines)}");
    }

}
