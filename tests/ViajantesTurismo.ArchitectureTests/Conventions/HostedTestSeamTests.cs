using ViajantesTurismo.ArchitectureTests.Repository;
using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class HostedTestSeamTests
{
    [Fact]
    public void Hosted_transport_tests_do_not_construct_persistence_adapters()
    {
        // Arrange
        var root = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "tests", "ViajantesTurismo.HostedTests");

        // Act
        var violations = HostedTestSeamPolicy.FindDirectPersistenceViolations(root);

        // Assert
        violations.ShouldBeEmpty("Use the documented HTTP, browser, scanner, or object-storage entrypoint. "
            + "Keep PostgreSQL tests in their declared persistence seam and permitted setup in typed scenario helpers.");
    }

    [Fact]
    public void Catalog_media_transport_tests_cannot_open_postgresql_directly()
    {
        // Arrange
        const string mediaTest = "Catalog/Media/MediaInfrastructureContainerTests.cs";
        const string databaseTestSource = """
            [TestScope(TestScope.Integration)]
            class ExampleTests
            {
                [Fact]
                public void Opens_database() { using var connection = new NpgsqlConnection(connectionString); }
            }
            """;
        using var workspace = new TemporaryWorkspace();
        workspace.WriteFile("Admin/Api/README.md", "");
        workspace.WriteFile("Admin/System/README.md", "");
        workspace.WriteFile("Management/Browser/README.md", "");
        workspace.WriteFile(mediaTest, databaseTestSource);
        workspace.WriteFile("Catalog/Infrastructure/Media/MediaUploadPostgreSqlTests.cs", databaseTestSource);
        workspace.WriteFile("Catalog/Media/SeaweedFsWebIdentityTests.cs", """
            [TestScope(TestScope.Integration)]
            class MediaTests
            {
                [Fact]
                public void Uses_storage() { var endpoint = fixture.SeaweedFsEndpoint; }
            }
            """);

        // Act
        var violations = HostedTestSeamPolicy.FindDirectPersistenceViolations(workspace.RootPath);

        // Assert
        violations.ShouldBe([mediaTest]);
    }

    [Theory]
    [InlineData("var connection = new NpgsqlConnection(connectionString);", true)]
    [InlineData("var connection = new global::Npgsql.NpgsqlConnection (connectionString);", true)]
    [InlineData("var context = new AdminWriteDbContext\n(options);", true)]
    [InlineData("var source = global::Npgsql.NpgsqlDataSource.Create(connectionString);", true)]
    [InlineData("var source = new NpgsqlDataSourceBuilder(connectionString).Build();", true)]
    [InlineData("var result = await fixture.CreateTour(command, ct);", false)]
    [InlineData("// new NpgsqlConnection(connectionString)", false)]
    [InlineData("var example = \"new NpgsqlConnection(connectionString)\";", false)]
    public void Hosted_persistence_guard_distinguishes_adapter_access_from_fixture_usage_and_examples(string code, bool expected)
    {
        // Arrange
        var source = "class Example { void Test() {\n" + code + "\n} }";

        // Act
        var violation = HostedTestSeamPolicy.UsesDirectPersistence(source);

        // Assert
        violation.ShouldBe(expected);
    }

    [Fact]
    public void System_tests_use_the_shared_application_fixture()
    {
        // Arrange
        var root = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "tests", "ViajantesTurismo.HostedTests", "Admin", "System");

        // Act
        var violations = Directory.GetFiles(root, "*Tests.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedTestPath(path))
            .Where(path => AdminSystemFixturePolicy.HasMissingFixture(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        // Assert
        violations.ShouldBeEmpty("Use AspireSystemTestFixture through IClassFixture or AspireSystemTestBase.");
    }

    [Theory]
    [InlineData("[TestScope(TestScope.System)] class JourneyTests : AspireSystemTestBase<AspireSystemTestFixture> { }", false)]
    [InlineData("[TestScope(TestScope.System)] class JourneyTests : IClassFixture<AspireSystemTestFixture> { }", false)]
    [InlineData(
        "class JourneyTests : global::Xunit.IClassFixture<Fixtures.AspireSystemTestFixture> { "
        + "[global::SharedKernel.Testing.TestScopeAttribute(SharedKernel.Testing.TestScope.System)] [Fact] public void Runs_the_journey() { } }",
        false)]
    [InlineData("[TestScope(TestScope.System)] class JourneyTests(AspireSystemTestFixture fixture) { }", true)]
    [InlineData("[TestScope(TestScope.System)] class JourneyTests : IClassFixture<OtherFixture> { } // AspireSystemTestFixture", true)]
    [InlineData("[TestScope(TestScope.System)] class JourneyTests { } class OtherTests : IClassFixture<AspireSystemTestFixture> { }", true)]
    [InlineData("class JourneyTests { [TestScopeAttribute(TestScope.System)] [Fact] public void Runs_the_journey() { } }", true)]
    [InlineData("[TestScope(TestScope.Integration)] class EndpointTests { } // [TestScope(TestScope.System)]", false)]
    public void System_scope_requires_the_shared_fixture_on_its_own_class(string source, bool expectedViolation)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(source);

        // Act
        var missingFixture = AdminSystemFixturePolicy.HasMissingFixture(source);

        // Assert
        missingFixture.ShouldBe(expectedViolation);
    }
}
