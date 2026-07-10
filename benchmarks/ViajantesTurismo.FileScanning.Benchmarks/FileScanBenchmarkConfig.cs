using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace ViajantesTurismo.FileScanning.Benchmarks;

/// <summary>
/// Configures file scanning benchmark output with allocation and throughput columns.
/// </summary>
internal sealed class FileScanBenchmarkConfig : ManualConfig
{
    /// <summary>
    /// Initializes the file scanning benchmark output configuration.
    /// </summary>
    public FileScanBenchmarkConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.P95);
        AddColumn(new FileScanThroughputColumn());
    }
}
