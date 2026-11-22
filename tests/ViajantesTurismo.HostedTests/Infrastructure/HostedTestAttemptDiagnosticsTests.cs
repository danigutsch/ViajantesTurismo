using System.Globalization;
using System.Text.Json;
using PandaBear.IntegrationTesting;
using PandaBear.Testing;
using Xunit.Sdk;
using Xunit.v3;

namespace ViajantesTurismo.HostedTests.Infrastructure;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[TestScope(TestScope.Unit)]
public sealed class HostedTestAttemptDiagnosticsTests
{
    [Fact]
    public async Task Records_bounded_attempt_evidence_without_theory_input_or_test_output()
    {
        // Arrange
        const string privateMarker = "synthetic-theory-input";
        const string method = "Runs_a_sample_scenario";
        const string assemblyId = "assembly";
        const string collectionId = "collection";
        const string classId = "class";
        const string methodId = "method";
        const string caseId = "case";
        var testClass = new string('C', 300);
        var startedUtc = DateTimeOffset.UnixEpoch;
        var finishedUtc = startedUtc.AddSeconds(1);
        var traits = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
        var attachments = new Dictionary<string, TestAttachment>(StringComparer.Ordinal);
        await using var workspace = TestWorkspace.Create("vt-test-attempt-diagnostics");
        var directory = workspace.Folder("attempts");
        var reporter = new HostedTestAttemptDiagnostics(NullMessageSink.Instance, directory);

        // Act
        reporter.OnMessage(new TestCaseStarting
        {
            AssemblyUniqueID = assemblyId,
            TestCollectionUniqueID = collectionId,
            TestClassUniqueID = classId,
            TestMethodUniqueID = methodId,
            TestCaseUniqueID = caseId,
            StartTime = startedUtc,
            Explicit = false,
            SkipReason = null,
            SourceFilePath = null,
            SourceLineNumber = null,
            TestClassMetadataToken = null,
            TestClassName = testClass,
            TestClassNamespace = null,
            TestClassSimpleName = testClass,
            TestMethodArity = null,
            TestMethodMetadataToken = null,
            TestMethodName = method,
            TestMethodParameterTypesVSTest = null,
            TestMethodReturnTypeVSTest = null,
            TestCaseDisplayName = privateMarker,
            Traits = traits,
        });
        for (var index = 0; index < 12; index++)
        {
            var testId = index.ToString(CultureInfo.InvariantCulture);
            reporter.OnMessage(new TestStarting
            {
                AssemblyUniqueID = assemblyId,
                TestCollectionUniqueID = collectionId,
                TestClassUniqueID = classId,
                TestMethodUniqueID = methodId,
                TestCaseUniqueID = caseId,
                TestUniqueID = testId,
                TestDisplayName = privateMarker,
                TestLabel = privateMarker,
                StartTime = index == 0 ? startedUtc.AddSeconds(-1) : startedUtc,
                Explicit = false,
                Timeout = 0,
                Traits = traits,
            });
            if (index == 1)
            {
                reporter.OnMessage(new TestCleanupFailure
                {
                    AssemblyUniqueID = assemblyId,
                    TestCollectionUniqueID = collectionId,
                    TestClassUniqueID = classId,
                    TestMethodUniqueID = methodId,
                    TestCaseUniqueID = caseId,
                    TestUniqueID = testId,
                    ExceptionTypes = [nameof(InvalidOperationException)],
                    ExceptionParentIndices = [-1],
                    Messages = [privateMarker],
                    StackTraces = [privateMarker],
                });
            }
            else if (index > 1)
            {
                reporter.OnMessage(new TestFailed
                {
                    AssemblyUniqueID = assemblyId,
                    TestCollectionUniqueID = collectionId,
                    TestClassUniqueID = classId,
                    TestMethodUniqueID = methodId,
                    TestCaseUniqueID = caseId,
                    TestUniqueID = testId,
                    Cause = FailureCause.Assertion,
                    ExecutionTime = 1,
                    FinishTime = finishedUtc,
                    Output = privateMarker,
                    Warnings = [privateMarker],
                    ExceptionTypes = [nameof(InvalidOperationException)],
                    ExceptionParentIndices = [-1],
                    Messages = [privateMarker],
                    StackTraces = [privateMarker],
                });
            }

            reporter.OnMessage(new TestFinished
            {
                AssemblyUniqueID = assemblyId,
                TestCollectionUniqueID = collectionId,
                TestClassUniqueID = classId,
                TestMethodUniqueID = methodId,
                TestCaseUniqueID = caseId,
                TestUniqueID = testId,
                FinishTime = finishedUtc,
                Output = privateMarker,
                Warnings = [privateMarker],
                ExecutionTime = 1,
                Attachments = attachments,
            });
        }

        // Assert
        var records = Directory.GetFiles(directory, "*.json");
        records.Length.ShouldBe(11);
        var truncation = Path.Combine(directory, "truncation.json");
        records.ShouldContain(truncation);
        var attemptIds = new HashSet<string>(StringComparer.Ordinal);
        var cleanupCount = 0;
        foreach (var record in records.Where(path => !string.Equals(path, truncation, StringComparison.Ordinal)))
        {
            var json = await File.ReadAllTextAsync(record, TestContext.Current.CancellationToken);
            json.ShouldNotContain(privateMarker, StringComparison.Ordinal);
            using var parsed = JsonDocument.Parse(json);
            var attempt = parsed.RootElement;
            var id = attempt.GetProperty("attemptId").GetString().ShouldNotBeNull();
            Guid.TryParseExact(id, "N", out _).ShouldBeTrue();
            id.ShouldBe(Path.GetFileNameWithoutExtension(record));
            attemptIds.Add(id).ShouldBeTrue();
            attempt.GetProperty("testClass").GetString().ShouldBe(testClass[..256]);
            attempt.GetProperty("testMethod").GetString().ShouldBe(method);
            attempt.GetProperty("startedUtc").GetDateTimeOffset().ShouldBe(startedUtc);
            attempt.GetProperty("outcome").GetString().ShouldBe("failed");
            if (attempt.GetProperty("cleanupFailed").GetBoolean())
            {
                cleanupCount++;
            }
        }

        cleanupCount.ShouldBe(1);
    }
}
