using PandaBear.Blazor;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

/// <summary>Records handled UI failures reported by pages and layouts.</summary>
internal sealed class RecordingUiFailureReporter : IUiFailureReporter
{
    private readonly List<(string ExceptionType, string Operation)> reports = [];

    /// <summary>The exception type name and operation of each report, in order.</summary>
    public IReadOnlyList<(string ExceptionType, string Operation)> Reports => reports;

    /// <inheritdoc />
    public void Report(Exception exception, string operation)
    {
        reports.Add((exception.GetType().Name, operation));
    }
}
