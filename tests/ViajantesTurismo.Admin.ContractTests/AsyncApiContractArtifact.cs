using ViajantesTurismo.Admin.ContractTests.Infrastructure;

namespace ViajantesTurismo.Admin.ContractTests;

internal static class AsyncApiContractArtifact
{
    public static string Read()
        => File.ReadAllText(Path.Combine(ContractTestRepository.RootPath, "docs", "asyncapi.yaml"));
}
