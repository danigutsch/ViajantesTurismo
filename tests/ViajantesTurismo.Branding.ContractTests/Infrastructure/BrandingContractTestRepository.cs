namespace ViajantesTurismo.Branding.ContractTests.Infrastructure;

/// <summary>Locates the repository that holds the Branding contract artifacts.</summary>
internal static class BrandingContractTestRepository
{
    private static readonly Lazy<string> RootPathSource = new(FindRootPath);

    /// <summary>Gets the repository root, found by walking up to the directory containing <c>ViajantesTurismo.slnx</c>.</summary>
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
