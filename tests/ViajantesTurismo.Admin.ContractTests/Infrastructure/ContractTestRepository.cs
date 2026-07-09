namespace ViajantesTurismo.Admin.ContractTests.Infrastructure;

internal static class ContractTestRepository
{
    public static string RootPath
    {
        get
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
}
