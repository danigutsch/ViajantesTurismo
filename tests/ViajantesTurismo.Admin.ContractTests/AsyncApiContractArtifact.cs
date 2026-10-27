using ViajantesTurismo.Admin.ContractTests.Infrastructure;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Provides access to the canonical AsyncAPI contract artifact.
/// </summary>
internal static class AsyncApiContractArtifact
{
    /// <summary>
    /// Reads the contents of docs/asyncapi.json from the repository root.
    /// </summary>
    public static string Read()
        => File.ReadAllText(Path.Combine(ContractTestRepository.RootPath, "docs", "asyncapi.json"));
}
