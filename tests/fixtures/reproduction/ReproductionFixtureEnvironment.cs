using System.Reflection;
using SharedKernel.IO;
using SharedKernel.Testing;

namespace ViajantesTurismo.Testing.Reproduction;

/// <summary>Supplies source metadata and opt-in local export policy for ViajantesTurismo fixtures.</summary>
internal static class ReproductionFixtureEnvironment
{
    private const string ExportSetting = "VIAJANTESTURISMO_REPRODUCTION_MANIFESTS";

    /// <summary>Reads the source revision recorded by the fixture's build.</summary>
    internal static string GetSourceRevision(Assembly fixtureAssembly)
    {
        ArgumentNullException.ThrowIfNull(fixtureAssembly);
        var version = fixtureAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var delimiter = version?.LastIndexOfAny(['+', '.']) ?? -1;
        var revision = delimiter < 0 ? null : version?[(delimiter + 1)..];
        return IsRevision(revision)
            ? revision
            : throw new InvalidOperationException("Reproduction requires a test assembly built from a Git checkout.");
    }

    /// <summary>Resolves the opt-in local test results directory from the fixture build's checkout.</summary>
    internal static string? GetLocalResultsDirectory()
    {
        if (Environment.GetEnvironmentVariable(ExportSetting) != "1")
        {
            return null;
        }

        var repositoryRoot = RepositoryRoot.TryLocate("ViajantesTurismo.slnx")
            ?? throw new InvalidOperationException("Reproduction exports require the repository's local TestResults directory.");
        return RepositoryPath.Parse("TestResults/reproduction").ResolveUnder(repositoryRoot);
    }

    private static bool IsRevision([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? value) =>
        value is { Length: 40 or 64 } && IsNonzeroLowerHex(value);

    private static bool IsNonzeroLowerHex(string value) =>
        value.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f')
        && value.Any(static character => character != '0');
}
