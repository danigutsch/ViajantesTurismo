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
        // Arrange
        var root = Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.HostedTests");
        var owner = File.ReadAllText(Path.Combine(root, "Infrastructure", "AssemblyFixture.cs"));
        var fixture = File.ReadAllText(Path.Combine(root, "Infrastructure", "HostedTestFixture.cs"));
        var api = File.ReadAllText(Path.Combine(root, "Admin", "Api", "Infrastructure", "ApiFixture.cs"));
        var browser = File.ReadAllText(Path.Combine(root, "Admin", "System", "Infrastructure", "Bases", "AspireSystemTestBase.cs"));

        // Act
        var fixtureDeclarations = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(File.ReadAllLines)
            .Where(line => line.StartsWith("[assembly: AssemblyFixture(", StringComparison.Ordinal))
            .ToArray();

        // Assert
        fixtureDeclarations.ShouldHaveSingleItem();
        owner.ShouldContain("[assembly: AssemblyFixture(typeof(HostedTestFixture))]", StringComparison.Ordinal);
        fixture.ShouldContain("PostgreSqlTestDatabasePool", StringComparison.Ordinal);
        fixture.ShouldContain("builder.AddDatabaseServer(includePgWeb: false)", StringComparison.Ordinal);
        fixture.ShouldContain("builder.AddCache(includeRedisInsight: false)", StringComparison.Ordinal);
        api.ShouldContain("shared.GetApplication(HostedProfile.Admin", StringComparison.Ordinal);
        api.ShouldNotContain("ResetToKnownBaseline", StringComparison.Ordinal);
        browser.ShouldContain("IClassFixture<AspireSystemTestFixture>", StringComparison.Ordinal);
        browser.ShouldContain("protected Uri ApiBaseUri => ApiClient.BaseAddress", StringComparison.Ordinal);
    }

    [Fact]
    public void SystemTests_should_use_repository_parallelism_without_a_local_override()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();
        var systemTestsPath = Path.Combine(repositoryRoot, "tests", "ViajantesTurismo.HostedTests", "Admin", "System");
        var runnerConfigurationPath = Path.Combine(repositoryRoot, "tests", "xunit.runner.json");
        var localRunnerConfigurationPaths = Directory.GetFiles(
            systemTestsPath,
            "*xunit.runner.json",
            SearchOption.TopDirectoryOnly);

        // Act
        var violatingLines = Directory.GetFiles(systemTestsPath, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(path => File.ReadAllLines(path)
                .Select((line, index) => (line, index))
                .Where(candidate =>
                    candidate.line.Contains("DisableTestParallelization = true", StringComparison.Ordinal) ||
                    candidate.line.Contains("DisableParallelization = true", StringComparison.Ordinal))
                .Select(candidate =>
                    $"{Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')}:L{candidate.index + 1} {candidate.line.Trim()}"))
            .ToArray();
        var runnerConfiguration = File.ReadAllText(runnerConfigurationPath);

        // Assert
        violatingLines.ShouldBeEmpty();
        runnerConfiguration.ShouldContain("\"parallelizeTestCollections\": true", StringComparison.Ordinal);
        runnerConfiguration.ShouldContain("\"maxParallelThreads\": 0", StringComparison.Ordinal);
        localRunnerConfigurationPaths.ShouldBeEmpty();
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
        var systemTestBasePath = Path.Combine(
            repositoryRoot,
            "tests",
            "ViajantesTurismo.HostedTests", "Admin", "System",
            "Infrastructure",
            "Bases",
            "AspireSystemTestBase.cs");

        // Act
        var appMarkup = File.ReadAllText(appPath);
        var mainLayoutMarkup = File.ReadAllText(mainLayoutPath);
        var appStyles = File.ReadAllText(appStylesPath);
        var noScriptStylesExists = File.Exists(noScriptStylesPath);
        var noScriptStyles = noScriptStylesExists ? File.ReadAllText(noScriptStylesPath) : string.Empty;
        var interactiveReadyExists = File.Exists(interactiveReadyPath);
        var systemTestBase = File.ReadAllText(systemTestBasePath);
        var pagesWithRouteRenderModes = Directory.GetFiles(pagesPath, "*.razor", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("@rendermode", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .ToArray();

        // Assert
        appMarkup.ShouldContain("<HeadOutlet @rendermode=\"InteractiveServer\" />", StringComparison.Ordinal);
        appMarkup.ShouldContain("<Routes @rendermode=\"InteractiveServer\" />", StringComparison.Ordinal);
        appMarkup.ShouldContain("<div class=\"app-startup-status\" role=\"status\" aria-live=\"polite\">", StringComparison.Ordinal);
        appMarkup.ShouldContain("<noscript>", StringComparison.Ordinal);
        appMarkup.ShouldContain("<link rel=\"stylesheet\" href=\"@Assets[\"app-noscript.css\"]\" />", StringComparison.Ordinal);
        mainLayoutMarkup.ShouldContain("inert=\"@(!RendererInfo.IsInteractive ? \"inert\" : null)\"", StringComparison.Ordinal);
        mainLayoutMarkup.ShouldContain("aria-busy=\"@(!RendererInfo.IsInteractive ? \"true\" : \"false\")\"", StringComparison.Ordinal);
        mainLayoutMarkup.ShouldContain("data-interactive=\"@RendererInfo.IsInteractive.ToString().ToLowerInvariant()\"", StringComparison.Ordinal);
        appStyles.ShouldContain("body:has(.page[data-interactive=\"true\"]) .app-startup-status", StringComparison.Ordinal);
        systemTestBase.ShouldContain(".page[data-interactive=\\\"true\\\"]", StringComparison.Ordinal);
        systemTestBase.ShouldContain("DeveloperExceptionPageSelector", StringComparison.Ordinal);
        systemTestBase.ShouldNotContain(".page:not([inert])", StringComparison.Ordinal);
        systemTestBase.ShouldNotContain("maxAttempts", StringComparison.Ordinal);
        noScriptStylesExists.ShouldBeTrue();
        noScriptStyles.ShouldContain(".app-startup-status { display: none; }", StringComparison.Ordinal);
        interactiveReadyExists.ShouldBeFalse();
        pagesWithRouteRenderModes.ShouldBeEmpty();
    }

    [Fact]
    public void Customer_wizard_forms_should_have_unique_form_names()
    {
        // Arrange
        var createPagesPath = Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.Management.Web",
            "Components",
            "Pages",
            "Customers",
            "Create");
        Dictionary<string, string> expectedFormNames = new(StringComparer.Ordinal)
        {
            ["Accommodation.razor"] = "createCustomerAccommodation",
            ["Address.razor"] = "createCustomerAddress",
            ["Contact.razor"] = "createCustomerContact",
            ["EmergencyContact.razor"] = "createCustomerEmergencyContact",
            ["Identification.razor"] = "createCustomerIdentification",
            ["Medical.razor"] = "createCustomerMedical",
            ["PersonalInfo.razor"] = "createCustomerPersonalInfo",
            ["Physical.razor"] = "createCustomerPhysical",
        };

        foreach (var expectedFormName in expectedFormNames)
        {
            // Act
            var pageMarkup = File.ReadAllText(Path.Combine(createPagesPath, expectedFormName.Key));

            // Assert
            pageMarkup.ShouldContain($"FormName=\"{expectedFormName.Value}\"", StringComparison.Ordinal);
        }
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

        var areEqual = undocumentedSerialDefinitions.Length == 0;
        areEqual.ShouldBeTrue(
            $"Expected every DisableParallelization = true collection definition to include a nearby [SerialTestJustification] attribute, but found:{Environment.NewLine}{string.Join(Environment.NewLine, undocumentedSerialDefinitions)}");
    }

    [Fact]
    public void Admin_hosted_test_infrastructure_should_not_expose_generic_serviceprovider_reach_through()
    {
        var infrastructureRoots = new[]
        {
            Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.HostedTests", "Admin", "Api", "Infrastructure"),
            Path.Combine(GetRepositoryRoot(), "tests", "ViajantesTurismo.HostedTests", "Admin", "System", "Infrastructure")
        };

        var offendingMembers = infrastructureRoots
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(path => FindGenericServiceProviderReachThrough(path))
            .ToArray();

        var areDifferent = offendingMembers.Length != 0;
        areDifferent.ShouldBeFalse(
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

        var areDifferent1 = offendingLines.Length != 0;
        areDifferent1.ShouldBeFalse(
            $"Expected concrete test methods to use typed helpers instead of raw DI/scope plumbing, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingLines)}");
    }

    [Fact]
    public void Admin_module_test_helpers_should_return_typed_scopes()
    {
        // Arrange
        var helperPath = Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "ViajantesTurismo.Admin.UnitTests",
            "Infrastructure",
            "AdminInfrastructureModuleTestServices.cs");

        // Act
        var helperText = File.ReadAllText(helperPath);

        // Assert
        helperText.ShouldNotContain("public static ServiceProvider", StringComparison.Ordinal);
    }

    [Fact]
    public void Test_trait_names_should_use_canonical_constants()
    {
        var testsRoot = Path.Combine(GetRepositoryRoot(), "tests");
        var offendingLines = Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .SelectMany(FindHardcodedCanonicalTraitNames)
            .ToArray();

        var areDifferent2 = offendingLines.Length != 0;
        areDifferent2.ShouldBeFalse(
            $"Expected reusable trait names to come from canonical constants directly, but found:{Environment.NewLine}{string.Join(Environment.NewLine, offendingLines)}");
    }

}
