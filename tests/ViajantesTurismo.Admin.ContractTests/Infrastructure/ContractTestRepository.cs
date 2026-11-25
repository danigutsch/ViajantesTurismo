using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.ContractTests.Infrastructure;

/// <summary>Locates the repository root so contract tests can read checked-in artifacts.</summary>
internal static class ContractTestRepository
{
    private static readonly Lazy<string> RootPathSource = new(() => RepositoryRoot.Locate("ViajantesTurismo.slnx"));

    /// <summary>Gets the repository root directory, located by walking up to <c>ViajantesTurismo.slnx</c>.</summary>
    public static string RootPath => RootPathSource.Value;
}
