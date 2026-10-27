using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using PandaBear.Tabular;

namespace ViajantesTurismo.FileScanning.Benchmarks;

/// <summary>
/// Compares decoding the received CSV bytes to text before parsing against parsing the bytes directly.
/// </summary>
/// <remarks>
/// <see cref="ParseBytes"/> is a prototype that keeps the <see cref="CsvRow"/> rules: lines end at LF or CRLF, every comma
/// separates fields, and fields are trimmed. It produces field arrays only, so it is the lower bound for a byte-based
/// parser in SharedKernel, not a drop-in replacement.
/// </remarks>
[Config(typeof(FileScanBenchmarkConfig))]
public class CsvParseBenchmarks
{
    private const int ColumnCount = 24;

    private byte[] _csv = [];

    /// <summary>
    /// Gets or sets the CSV payload size in bytes.
    /// </summary>
    [Params(65_536, 1_048_576)]
    public int CsvSizeBytes
    {
        get; set;
    }

    /// <summary>
    /// Creates deterministic CSV bytes and verifies the byte parser yields the same fields as the text parser.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _csv = CreateCsv(CsvSizeBytes);

        var expected = CsvDocument.Parse(Encoding.UTF8.GetString(_csv)).Value;
        var actual = ParseFields(_csv);
        if (expected.Rows.Count != actual.Length)
        {
            throw new InvalidOperationException("The byte parser returned a different row count.");
        }

        for (var row = 0; row < actual.Length; row++)
        {
            if (expected.Rows[row].Count != actual[row].Length)
            {
                throw new InvalidOperationException("The byte parser returned a different field count.");
            }

            for (var column = 0; column < actual[row].Length; column++)
            {
                if (!string.Equals(expected.Rows[row][column], actual[row][column], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The byte parser returned a different field value.");
                }
            }
        }
    }

    /// <summary>
    /// Decodes the bytes to text and parses it once, as the import does today per parse.
    /// </summary>
    /// <returns>The parsed row count.</returns>
    [Benchmark(Baseline = true)]
    public int DecodeThenParse() => CsvDocument.Parse(Encoding.UTF8.GetString(_csv)).Value.Rows.Count;

    /// <summary>
    /// Parses the same text three times, as the import conflict check, summary analysis, and command handler do.
    /// </summary>
    /// <returns>The parsed row count.</returns>
    [Benchmark]
    public int DecodeThenParseThreeTimes()
    {
        var text = Encoding.UTF8.GetString(_csv);
        var count = CsvDocument.Parse(text).Value.Rows.Count;
        count += CsvDocument.Parse(text).Value.Rows.Count;
        count += CsvDocument.Parse(text).Value.Rows.Count;
        return count;
    }

    /// <summary>
    /// Parses the bytes directly without the intermediate text or line strings.
    /// </summary>
    /// <returns>The parsed row count.</returns>
    [Benchmark]
    public int ParseBytes() => ParseFields(_csv).Length;

    // ponytail: simple comma split like PandaBear.Tabular.CsvRow; quoted and multiline fields are unsupported there too.
    private static string[][] ParseFields(ReadOnlySpan<byte> csv)
    {
        var rows = new List<string[]>();
        var fields = new List<string>(ColumnCount);
        while (true)
        {
            var lineEnd = csv.IndexOf((byte)'\n');
            var line = lineEnd < 0 ? csv : csv[..lineEnd];
            if (!line.IsEmpty && line[^1] == (byte)'\r')
            {
                line = line[..^1];
            }

            fields.Clear();
            while (true)
            {
                var comma = line.IndexOf((byte)',');
                var field = comma < 0 ? line : line[..comma];
                fields.Add(Encoding.UTF8.GetString(field).Trim());
                if (comma < 0)
                {
                    break;
                }

                line = line[(comma + 1)..];
            }

            rows.Add([.. fields]);
            if (lineEnd < 0)
            {
                return [.. rows.Skip(1)];
            }

            csv = csv[(lineEnd + 1)..];
        }
    }

    private static byte[] CreateCsv(int sizeBytes)
    {
        var builder = new StringBuilder(sizeBytes + 512);
        builder.AppendJoin(',', Enumerable.Range(0, ColumnCount).Select(column => string.Create(CultureInfo.InvariantCulture, $"Header{column}")));
        var row = 0;
        while (builder.Length < sizeBytes)
        {
            builder.Append('\n');
            builder.AppendJoin(',', Enumerable.Range(0, ColumnCount).Select(column => string.Create(CultureInfo.InvariantCulture, $"value-{row}-{column}")));
            row++;
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }
}
