using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace ViajantesTurismo.FileScanning.Benchmarks;

/// <summary>
/// Reports scanned file throughput in mebibytes per second for each benchmark case.
/// </summary>
internal sealed class FileScanThroughputColumn : IColumn
{
    /// <inheritdoc />
    public string Id => nameof(FileScanThroughputColumn);

    /// <inheritdoc />
    public string ColumnName => "MiB/s";

    /// <inheritdoc />
    public bool AlwaysShow => true;

    /// <inheritdoc />
    public ColumnCategory Category => ColumnCategory.Custom;

    /// <inheritdoc />
    public int PriorityInCategory => 0;

    /// <inheritdoc />
    public bool IsNumeric => true;

    /// <inheritdoc />
    public UnitType UnitType => UnitType.Dimensionless;

    /// <inheritdoc />
    public string Legend => "Mean scanned MiB per second, including repeated scans.";

    /// <inheritdoc />
    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(benchmarkCase);

        var report = summary[benchmarkCase];
        if (report is null)
        {
            return "n/a";
        }

        var meanNanoseconds = report.ResultStatistics?.Mean;

        if (meanNanoseconds is null or <= 0)
        {
            return "n/a";
        }

        var fileSizeBytes = Convert.ToInt64(benchmarkCase.Parameters[nameof(FileScanBenchmarks.FileSizeBytes)], CultureInfo.InvariantCulture);
        var scanCount = Convert.ToInt64(benchmarkCase.Parameters[nameof(FileScanBenchmarks.ScanCount)], CultureInfo.InvariantCulture);
        var scannedMebibytes = fileSizeBytes * scanCount / 1_048_576D;
        var meanSeconds = meanNanoseconds.Value / 1_000_000_000D;
        var throughput = scannedMebibytes / meanSeconds;

        return throughput.ToString("N1", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        return GetValue(summary, benchmarkCase);
    }

    /// <inheritdoc />
    public bool IsAvailable(Summary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return true;
    }

    /// <inheritdoc />
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(benchmarkCase);

        return false;
    }
}
