using SharedKernel.Testing;

namespace ViajantesTurismo.ArchitectureTests.Repository;

public sealed class UnpublishedTerminologyTests
{
    private const string TermListPath = ".config/unpublished-terminology.txt";

    // TODO(#1090): Remove this test and its term list in the first application publication PR.
    [Fact]
    public async Task Application_sources_avoid_unpublished_compatibility_terminology()
    {
        // Arrange
        var root = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var cancellationToken = TestContext.Current.CancellationToken;
        var terms = await File.ReadAllLinesAsync(Path.Combine(root, TermListPath), cancellationToken);
        terms.ShouldNotBeEmpty();
        terms.ShouldAllSatisfy(static term => string.IsNullOrWhiteSpace(term).ShouldBeFalse());

        // Act
        var result = await TestGit.TryRun(root,
            [
                "grep", "--untracked", "--line-number", "--only-matching", "-I", "--ignore-case", "--fixed-strings",
                "-f", TermListPath, "--", "src", "tests", "docs",
            ],
            cancellationToken);

        // Assert
        result.ExitCode.ShouldBe(1, $"Review the publication terminology findings or Git error.\n{result.StandardOutput}{result.StandardError}");
    }
}
