namespace ViajantesTurismo.Branding.ContractTests.Infrastructure;

internal static class BrandingContractTestRepository
{
    private static readonly Lazy<string> RootPathSource = new(FindRootPath);

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
