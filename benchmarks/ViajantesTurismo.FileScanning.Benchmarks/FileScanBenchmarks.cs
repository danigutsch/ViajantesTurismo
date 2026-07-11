using System.Globalization;
using System.IO.MemoryMappedFiles;
using BenchmarkDotNet.Attributes;
using Microsoft.Win32.SafeHandles;

namespace ViajantesTurismo.FileScanning.Benchmarks;

/// <summary>
/// Measures local file scanning strategies across one-pass and repeated scans.
/// </summary>
[Config(typeof(FileScanBenchmarkConfig))]
public class FileScanBenchmarks
{
    private const byte TargetByte = 0x5A;
    private const int ScanBufferLength = 128 * 1024;

    private byte[] _buffer = [];
    private string _filePath = string.Empty;

    /// <summary>
    /// Gets or sets the generated local file size in bytes.
    /// </summary>
    [Params(65_536, 4_194_304, 67_108_864)]
    public int FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of full file scans performed by each benchmark operation.
    /// </summary>
    [Params(1, 8)]
    public int ScanCount { get; set; }

    /// <summary>
    /// Creates deterministic benchmark input data under the ignored BenchmarkDotNet artifact path.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _buffer = GC.AllocateUninitializedArray<byte>(ScanBufferLength);

        var dataDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "BenchmarkDotNet.Artifacts", "file-scan-data"));
        Directory.CreateDirectory(dataDirectory);

        _filePath = Path.Combine(dataDirectory, "file-scan-" + FileSizeBytes.ToString(CultureInfo.InvariantCulture) + ".bin");
        WriteDeterministicFile(_filePath, FileSizeBytes);
    }

    /// <summary>
    /// Deletes the generated benchmark input data after each benchmark case.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (!string.IsNullOrWhiteSpace(_filePath) && File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }

        var dataDirectory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dataDirectory) && Directory.Exists(dataDirectory) && !Directory.EnumerateFileSystemEntries(dataDirectory).Any())
        {
            Directory.Delete(dataDirectory);
        }
    }

    /// <summary>
    /// Scans the file with the default <see cref="FileStream" /> sequential read path.
    /// </summary>
    /// <returns>The number of matched bytes across all scans.</returns>
    [Benchmark(Baseline = true, Description = "FileStream sequential")]
    public long FileStreamSequential()
    {
        long matches = 0;

        for (var scanIndex = 0; scanIndex < ScanCount; scanIndex++)
        {
            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4_096, FileOptions.SequentialScan);
            matches += ScanStream(stream);
        }

        return matches;
    }

    /// <summary>
    /// Scans the file with a larger <see cref="BufferedStream" /> over a minimally buffered file stream.
    /// </summary>
    /// <returns>The number of matched bytes across all scans.</returns>
    [Benchmark(Description = "Buffered FileStream sequential")]
    public long BufferedFileStreamSequential()
    {
        long matches = 0;

        for (var scanIndex = 0; scanIndex < ScanCount; scanIndex++)
        {
            using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, FileOptions.SequentialScan);
            using var bufferedStream = new BufferedStream(fileStream, ScanBufferLength);
            matches += ScanStream(bufferedStream);
        }

        return matches;
    }

    /// <summary>
    /// Scans the file through a memory-mapped view stream.
    /// </summary>
    /// <returns>The number of matched bytes across all scans.</returns>
    [Benchmark(Description = "MemoryMappedFile sequential")]
    public long MemoryMappedFileSequential()
    {
        long matches = 0;

        for (var scanIndex = 0; scanIndex < ScanCount; scanIndex++)
        {
            using var mappedFile = MemoryMappedFile.CreateFromFile(_filePath, FileMode.Open, mapName: null, capacity: 0, MemoryMappedFileAccess.Read);
            using var stream = mappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
            matches += ScanStream(stream);
        }

        return matches;
    }

    /// <summary>
    /// Scans the file with offset-based <see cref="RandomAccess" /> reads.
    /// </summary>
    /// <returns>The number of matched bytes across all scans.</returns>
    [Benchmark(Description = "RandomAccess sequential")]
    public long RandomAccessSequential()
    {
        long matches = 0;

        for (var scanIndex = 0; scanIndex < ScanCount; scanIndex++)
        {
            using SafeFileHandle handle = File.OpenHandle(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
            long offset = 0;

            while (offset < FileSizeBytes)
            {
                var requested = (int)Math.Min(_buffer.Length, FileSizeBytes - offset);
                var bytesRead = RandomAccess.Read(handle, _buffer.AsSpan(0, requested), offset);

                if (bytesRead == 0)
                {
                    break;
                }

                matches += CountMatches(_buffer.AsSpan(0, bytesRead));
                offset += bytesRead;
            }
        }

        return matches;
    }

    private static void WriteDeterministicFile(string path, int length)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, ScanBufferLength, FileOptions.SequentialScan);
        var writeBuffer = GC.AllocateUninitializedArray<byte>(ScanBufferLength);
        long written = 0;

        while (written < length)
        {
            var count = (int)Math.Min(writeBuffer.Length, length - written);
            FillDeterministic(writeBuffer.AsSpan(0, count), written);
            stream.Write(writeBuffer.AsSpan(0, count));
            written += count;
        }
    }

    private static void FillDeterministic(Span<byte> destination, long absoluteOffset)
    {
        for (var index = 0; index < destination.Length; index++)
        {
            destination[index] = unchecked((byte)(((absoluteOffset + index) * 31) + 17));
        }
    }

    private long ScanStream(Stream stream)
    {
        long matches = 0;

        while (true)
        {
            var bytesRead = stream.Read(_buffer.AsSpan(0, _buffer.Length));

            if (bytesRead == 0)
            {
                break;
            }

            matches += CountMatches(_buffer.AsSpan(0, bytesRead));
        }

        return matches;
    }

    private static long CountMatches(ReadOnlySpan<byte> source)
    {
        long matches = 0;

        foreach (var value in source)
        {
            if (value == TargetByte)
            {
                matches++;
            }
        }

        return matches;
    }
}
