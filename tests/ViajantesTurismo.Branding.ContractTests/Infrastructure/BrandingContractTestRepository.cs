using PandaBear.Testing;

namespace ViajantesTurismo.Branding.ContractTests.Infrastructure;

/// <summary>Locates the repository that holds the Branding contract artifacts.</summary>
internal static class BrandingContractTestRepository
{
    private static readonly Lazy<string> RootPathSource = new(() => RepositoryRoot.Locate("ViajantesTurismo.slnx"));

    /// <summary>Gets the repository root, found by walking up to the directory containing <c>ViajantesTurismo.slnx</c>.</summary>
    public static string RootPath => RootPathSource.Value;
}
