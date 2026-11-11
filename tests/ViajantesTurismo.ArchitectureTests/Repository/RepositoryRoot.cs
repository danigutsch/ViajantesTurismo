namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>Locates the repository checkout that the architecture tests run from.</summary>
internal static class RepositoryRoot
{
    /// <summary>Returns the nearest ancestor of the test binaries that holds <c>ViajantesTurismo.slnx</c>.</summary>
    /// <exception cref="DirectoryNotFoundException">Thrown when no ancestor holds the solution file.</exception>
    public static string Locate()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
