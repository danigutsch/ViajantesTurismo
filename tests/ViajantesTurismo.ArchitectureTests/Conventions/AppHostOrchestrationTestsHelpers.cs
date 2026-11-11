namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for tests that pin the AppHost orchestration source text.</summary>
internal static class AppHostOrchestrationTestsHelpers
{
    /// <summary>Returns the repository root: the nearest ancestor of the test output directory that holds ViajantesTurismo.slnx.</summary>
    public static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
