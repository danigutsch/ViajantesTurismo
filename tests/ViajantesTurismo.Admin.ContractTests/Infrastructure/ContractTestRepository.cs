namespace ViajantesTurismo.Admin.ContractTests.Infrastructure;

/// <summary>Locates the repository root so contract tests can read checked-in artifacts.</summary>
internal static class ContractTestRepository
{
    private static readonly Lazy<string> RootPathSource = new(FindRootPath);

    /// <summary>Gets the repository root directory, located by walking up to <c>ViajantesTurismo.slnx</c>.</summary>
    public static string RootPath => RootPathSource.Value;

    private static string FindRootPath()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, "ViajantesTurismo.slnx");
            if (File.Exists(candidatePath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root for contract test artifacts.");
    }
}
