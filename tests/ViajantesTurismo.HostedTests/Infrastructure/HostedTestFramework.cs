using System.Reflection;
using Xunit.v3;

namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>Enables opt-in attempt evidence while delegating execution to the standard xUnit framework.</summary>
public sealed class HostedTestFramework : XunitTestFramework
{
    private readonly string? configFileName;

    /// <summary>Creates the hosted test framework with the runner's default configuration.</summary>
    public HostedTestFramework()
    {
    }

    /// <summary>Creates the hosted test framework with the selected runner configuration.</summary>
    public HostedTestFramework(string? configFileName) : base(configFileName)
    {
        this.configFileName = configFileName;
    }

    /// <inheritdoc/>
    protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly)
    {
        var directory = Environment.GetEnvironmentVariable("VT_TEST_ATTEMPT_DIAGNOSTICS_DIR");
        return string.IsNullOrWhiteSpace(directory)
            ? base.CreateExecutor(assembly)
            : new HostedTestEvidenceExecutor(new XunitTestAssembly(assembly, configFileName), directory);
    }
}
