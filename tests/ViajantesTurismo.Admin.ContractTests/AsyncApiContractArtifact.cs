namespace ViajantesTurismo.Admin.ContractTests;

internal static class AsyncApiContractArtifact
{
    public static string Read()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "ViajantesTurismo.slnx");

            if (File.Exists(solutionPath))
            {
                return File.ReadAllText(Path.Combine(directory.FullName, "docs", "asyncapi.yaml"));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located from the test output directory.");
    }
}
