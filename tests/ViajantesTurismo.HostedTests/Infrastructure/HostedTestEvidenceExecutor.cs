using Xunit.Sdk;
using Xunit.v3;

namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>Observes xUnit messages for one hosted test execution.</summary>
/// <param name="testAssembly">The xUnit assembly to execute.</param>
/// <param name="directory">The dedicated directory for bounded attempt evidence.</param>
internal sealed class HostedTestEvidenceExecutor(IXunitTestAssembly testAssembly, string directory)
    : XunitTestFrameworkExecutor(testAssembly)
{
    /// <inheritdoc/>
    public override ValueTask RunTestCases(
        IReadOnlyCollection<IXunitTestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        CancellationToken cancellationToken) =>
        base.RunTestCases(testCases, new HostedTestAttemptDiagnostics(executionMessageSink, directory),
            executionOptions, cancellationToken);
}
