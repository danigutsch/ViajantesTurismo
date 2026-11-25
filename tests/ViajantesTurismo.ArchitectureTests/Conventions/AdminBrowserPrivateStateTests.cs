using ViajantesTurismo.ArchitectureTests.Repository;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class AdminBrowserPrivateStateTests
{
    [Fact]
    public void Admin_browser_tests_stay_at_their_visible_or_reviewed_state_boundary()
    {
        // Arrange
        var root = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var systemRoot = Path.Combine(root, "tests", "ViajantesTurismo.HostedTests", "Admin", "System");
        var exceptions = Path.Combine(root, ".config", "hosted-browser-state-exceptions.json");

        // Act
        var violations = AdminBrowserPrivateStatePolicy.FindViolations(systemRoot, exceptions).ToArray();

        // Assert
        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void A_direct_browser_base_still_uses_the_browser_boundary()
    {
        // Arrange
        using var workspace = new TemporaryWorkspace();
        workspace.WriteFile("Source/BrowserTests.cs", """
            class BrowserTests : global::Microsoft.Playwright.Xunit.v3.BrowserTest, IClassFixture<AspireSystemTestFixture>
            {
                [Fact]
                public void Checks_boundary() { Fixture.CountCatalogTourEvents(id, ct); }
            }
            """);
        workspace.WriteFile("exceptions.json", "[]");

        // Act
        var violations = AdminBrowserPrivateStatePolicy.FindViolations(
            Path.Combine(workspace.RootPath, "Source"), Path.Combine(workspace.RootPath, "exceptions.json")).ToArray();

        // Assert
        violations.ShouldHaveSingleItem().ShouldContain("Checks_boundary accesses CountCatalogTourEvents", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("GetRejectedDocumentReadAuditActor", 1, false, false)]
    [InlineData("GetRejectedDocumentReadAuditActor", 2, true, false)]
    [InlineData("CountCatalogTourEvents", 1, true, true)]
    [InlineData("CreateCatalogApiClient", 1, false, true)]
    public void A_reviewed_lookup_allows_only_one_matching_operation(
        string operation,
        int calls,
        bool expectedAccessFinding,
        bool expectedUnusedReview)
    {
        // Arrange
        using var workspace = new TemporaryWorkspace();
        var body = string.Concat(Enumerable.Repeat($"backend.{operation}(value, ct);\n", calls));
        workspace.WriteFile("Source/BrowserTests.cs", $$"""
            class BrowserTests : global::Fixtures.AspireSystemTestBase<AspireSystemTestFixture>
            {
                [Fact]
                public void Checks_boundary() { var backend = Fixture; {{body}} }
            }
            class ReplayTests : IClassFixture<AspireSystemTestFixture>
            {
                [Fact]
                public void Replays_events() { fixture.CountCatalogTourEvents(id, ct); }
            }
            """);
        workspace.WriteFile("exceptions.json", """
            [{
              "file": "BrowserTests.cs",
              "class": "BrowserTests",
              "test": "Checks_boundary",
              "operation": "GetRejectedDocumentReadAuditActor",
              "review": "https://example.com/review/1",
              "reason": "The audited subject is part of this conformance proof."
            }]
            """);

        // Act
        var violations = AdminBrowserPrivateStatePolicy.FindViolations(
            Path.Combine(workspace.RootPath, "Source"), Path.Combine(workspace.RootPath, "exceptions.json")).ToArray();

        // Assert
        var accessFindings = violations.Count(violation => violation.Contains($"accesses {operation}", StringComparison.Ordinal));
        var unusedReviews = violations.Count(violation => violation.StartsWith("Remove the unused", StringComparison.Ordinal));
        accessFindings.ShouldBe(expectedAccessFinding ? 1 : 0);
        unusedReviews.ShouldBe(expectedUnusedReview ? 1 : 0);
        violations.Length.ShouldBe(accessFindings + unusedReviews);
    }
}
