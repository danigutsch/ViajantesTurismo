using System.Globalization;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ViajantesTurismo.RepoConfig.Tool;

internal static partial class CoverageGateCommand
{
    internal static async Task<int> Run(string[] args, TextWriter output, TextWriter error, string workingDirectory, CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || args[index] is not ("--root" or "--report" or "--diff" or "--output") || !options.TryAdd(args[index], args[index + 1]))
            {
                throw new ArgumentException("Expected --report <Cobertura.xml> --diff <changes.patch> [--root <path>] [--output <path>].");
            }
        }
        var root = Path.GetFullPath(options.GetValueOrDefault("--root", workingDirectory), workingDirectory);
        var report = Path.GetFullPath(options.GetValueOrDefault("--report") ?? throw new ArgumentException("Missing --report."), root);
        var diff = Path.GetFullPath(options.GetValueOrDefault("--diff") ?? throw new ArgumentException("Missing --diff."), root);
        var evidence = Path.GetFullPath(options.GetValueOrDefault("--output", "TestResults/coverage-gate.json"), root);
        try
        {
            var changes = ReadChanges(await File.ReadAllLinesAsync(diff, cancellationToken).ConfigureAwait(false), root);
            var coverage = ReadCoverage(report, root);
            long total = 0;
            long covered = 0;
            foreach (var (path, changedLines) in changes)
            {
                if (!coverage.TryGetValue(path, out var lines))
                {
                    if (HasNonExecutableSymbols(path, root)) { continue; }
                    throw new InvalidOperationException($"Changed production source is missing from coverage: {path}");
                }
                foreach (var number in changedLines)
                {
                    if (!lines.TryGetValue(number, out var line)) { continue; }
                    total += 1L + line.Branches;
                    covered += (line.Hit ? 1L : 0L) + line.CoveredBranches;
                }
            }
            var passed = total == 0 || covered * 100m >= total * 80m;
            await WriteEvidence(evidence, passed, covered, total, null, cancellationToken).ConfigureAwait(false);
            await output.WriteLineAsync($"Coverage: {covered}/{total} changed executable lines and conditions; required 80%.".AsMemory(), cancellationToken).ConfigureAwait(false);
            return passed ? 0 : 1;
        }
        catch (Exception exception) when (exception is XmlException or IOException or InvalidOperationException or FormatException or OverflowException or BadImageFormatException)
        {
            await WriteEvidence(evidence, false, 0, 0, exception.Message, cancellationToken).ConfigureAwait(false);
            await error.WriteLineAsync($"Coverage evidence is invalid: {exception.Message}".AsMemory(), cancellationToken).ConfigureAwait(false);
            return 1;
        }
    }

    private static Dictionary<string, HashSet<int>> ReadChanges(string[] patch, string root)
    {
        var changes = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        string? path = null;
        var next = 0;
        var remaining = 0;
        var removedRemaining = 0;
        var inHunk = false;
        var hasFileHeader = false;
        var awaitingHunk = false;
        foreach (var text in patch)
        {
            if (text.StartsWith("+++ ", StringComparison.Ordinal) && (!inHunk || (remaining == 0 && removedRemaining == 0)))
            {
                if (remaining != 0 || removedRemaining != 0 || awaitingHunk) { throw new FormatException("Incomplete diff hunk."); }
                hasFileHeader = true;
                awaitingHunk = true;
                var name = text[4..].Split('\t', 2)[0];
                if (name.StartsWith('"')) { throw new FormatException("Quoted diff paths require git -c core.quotePath=false."); }
                path = name == "/dev/null" ? null : NormalizeSource(name.StartsWith("b/", StringComparison.Ordinal) ? name[2..] : name, root);
                if (name != "/dev/null" && path is null) { throw new FormatException("Diff source escapes repository root."); }
                inHunk = false;
                continue;
            }
            if (text.StartsWith("@@", StringComparison.Ordinal))
            {
                if (remaining != 0 || removedRemaining != 0) { throw new FormatException("Incomplete diff hunk."); }
                if (!hasFileHeader) { throw new FormatException("Diff hunk has no source header."); }
                awaitingHunk = false;
                var match = HunkPattern().Match(text);
                if (!match.Success) { throw new FormatException("Malformed diff hunk."); }
                next = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                remaining = match.Groups[4].Success ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : 1;
                removedRemaining = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 1;
                if ((remaining > 0 && next == 0) || (removedRemaining > 0 && match.Groups[1].Value == "0")) { throw new FormatException("Diff lines must start at one."); }
                inHunk = true;
                if (path is not null && IsProduction(path)) { changes.TryAdd(path, []); }
                continue;
            }
            if (!inHunk || text.StartsWith("\\ No newline", StringComparison.Ordinal)) { continue; }
            if (remaining == 0 && removedRemaining == 0) { inHunk = false; continue; }
            if (text.StartsWith('+'))
            {
                if (path is not null && IsProduction(path)) { changes[path].Add(next); }
                next++;
                remaining--;
            }
            else if (text.StartsWith('-')) { removedRemaining--; }
            else if (text.StartsWith(' ')) { next++; remaining--; removedRemaining--; }
            else { throw new FormatException("Invalid diff hunk content."); }
            if (remaining < 0 || removedRemaining < 0) { throw new FormatException("Diff hunk length mismatch."); }
        }
        if (remaining != 0 || removedRemaining != 0 || awaitingHunk) { throw new FormatException("Incomplete diff hunk."); }
        foreach (var key in changes.Where(pair => pair.Value.Count == 0).Select(pair => pair.Key).ToArray()) { changes.Remove(key); }
        if (patch.Length > 0 && !patch.Any(line => line.StartsWith("diff --git ", StringComparison.Ordinal) || line.StartsWith("--- ", StringComparison.Ordinal)))
        {
            throw new FormatException("Not a unified diff.");
        }
        return changes;
    }

    private static Dictionary<string, Dictionary<int, (bool Hit, int CoveredBranches, int Branches)>> ReadCoverage(string report, string root)
    {
        using var reader = XmlReader.Create(report, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
        var document = XDocument.Load(reader);
        if (document.Root?.Name != "coverage") { throw new FormatException("Expected Cobertura coverage root."); }
        var sources = document.Descendants("source").Select(element => element.Value).ToArray();
        var result = new Dictionary<string, Dictionary<int, (bool Hit, int CoveredBranches, int Branches)>>(StringComparer.Ordinal);
        foreach (var entry in document.Descendants("class"))
        {
            var filename = entry.Attribute("filename")?.Value ?? throw new FormatException("Coverage class is missing filename.");
            var path = NormalizeSource(filename, root);
            if (path is not null && !File.Exists(Path.Combine(root, path)))
            {
                path = sources.Select(source => NormalizeSource(Path.Combine(source, filename), root)).FirstOrDefault(candidate => candidate is not null && File.Exists(Path.Combine(root, candidate)));
            }
            if (path is null || !IsProduction(path)) { continue; }
            if (!result.TryGetValue(path, out var lines)) { lines = []; result.Add(path, lines); }
            foreach (var line in entry.Elements("lines").Elements("line"))
            {
                var number = int.Parse(line.Attribute("number")?.Value ?? throw new FormatException("Coverage line is missing number."), CultureInfo.InvariantCulture);
                var hits = long.Parse(line.Attribute("hits")?.Value ?? throw new FormatException("Coverage line is missing hits."), CultureInfo.InvariantCulture);
                if (number <= 0 || hits < 0) { throw new FormatException("Coverage numbers must be nonnegative and lines positive."); }
                var branches = 0;
                var coveredBranches = 0;
                if (line.Attribute("branch")?.Value == "true")
                {
                    var match = ConditionPattern().Match(line.Attribute("condition-coverage")?.Value ?? string.Empty);
                    if (!match.Success) { throw new FormatException("Malformed branch coverage."); }
                    coveredBranches = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    branches = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    if (coveredBranches > branches) { throw new FormatException("Covered branches exceed total branches."); }
                }
                lines.TryGetValue(number, out var previous);
                lines[number] = (previous.Hit || hits > 0, Math.Max(previous.CoveredBranches, coveredBranches), Math.Max(previous.Branches, branches));
            }
        }
        return result;
    }

    private static bool HasNonExecutableSymbols(string path, string root)
    {
        var checksum = SHA256.HashData(File.ReadAllBytes(Path.Combine(root, path)));
        var found = false;
        Span<byte> signature = stackalloc byte[4];
        foreach (var directory in new[] { "src", "tools" })
        {
            var sourceRoot = Path.Combine(root, directory);
            if (!Directory.Exists(sourceRoot)) { continue; }
            foreach (var filename in Directory.EnumerateFiles(sourceRoot, "*.pdb", SearchOption.AllDirectories)
                .Where(filename => filename.Replace('\\', '/').Contains("/bin/Release/", StringComparison.Ordinal)))
            {
                using var stream = File.OpenRead(filename);
                // Windows native symbols are not portable document evidence.
                if (stream.Read(signature) != 4 || !signature.SequenceEqual("BSJB"u8)) { continue; }
                stream.Position = 0;
                using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
                var reader = provider.GetMetadataReader();
                var documents = new HashSet<DocumentHandle>();
                foreach (var handle in reader.Documents)
                {
                    var document = reader.GetDocument(handle);
                    var name = reader.GetString(document.Name).Replace('\\', '/');
                    var documentPath = NormalizeSource(name.StartsWith("/_/", StringComparison.Ordinal) ? name[3..] : name, root);
                    if (documentPath != path) { continue; }
                    if (reader.GetGuid(document.HashAlgorithm) != new Guid("8829d00f-11b8-4213-878b-770e8597ac16")
                        || !reader.GetBlobBytes(document.Hash).AsSpan().SequenceEqual(checksum)) { return false; }
                    documents.Add(handle);
                    found = true;
                }
                if (documents.Count == 0) { continue; }
                foreach (var handle in reader.MethodDebugInformation)
                {
                    var method = reader.GetMethodDebugInformation(handle);
                    foreach (var point in method.GetSequencePoints())
                    {
                        var document = point.Document.IsNil ? method.Document : point.Document;
                        if (!point.IsHidden && documents.Contains(document)) { return false; }
                    }
                }
            }
        }
        return found;
    }

    private static string? NormalizeSource(string filename, string root)
    {
        var full = Path.GetFullPath(filename.Replace('\\', '/'), root);
        var relative = Path.GetRelativePath(root, full).Replace('\\', '/');
        return relative == ".." || relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative) ? null : relative;
    }

    private static bool IsProduction(string path) => (path.StartsWith("src/", StringComparison.Ordinal) || path.StartsWith("tools/", StringComparison.Ordinal))
        && (path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal))
        && !path.Contains("/Migrations/", StringComparison.Ordinal)
        && !path.Contains("/obj/", StringComparison.Ordinal)
        && !path.StartsWith("src/ViajantesTurismo.AppHost/", StringComparison.Ordinal)
        && !path.StartsWith("tools/ViajantesTurismo.Performance.Tool/", StringComparison.Ordinal)
        && path != "src/SharedKernel/SharedKernel.Mediator.SourceGenerator/IsExternalInit.cs"
        && !path.EndsWith(".g.cs", StringComparison.Ordinal)
        && !path.EndsWith(".generated.cs", StringComparison.Ordinal);

    private static async Task WriteEvidence(string filename, bool passed, long covered, long total, string? error, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filename) ?? throw new ArgumentException("Evidence requires a parent directory."));
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteBoolean("passed", passed);
            writer.WriteNumber("thresholdPercent", 80);
            writer.WriteNumber("covered", covered);
            writer.WriteNumber("total", total);
            if (error is not null) { writer.WriteString("error", error); }
            writer.WriteEndObject();
        }
        await File.WriteAllBytesAsync(filename, buffer.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    [GeneratedRegex("^@@ -([0-9]+)(?:,([0-9]+))? \\+([0-9]+)(?:,([0-9]+))? @@(?:.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex HunkPattern();

    [GeneratedRegex("^[0-9]+(?:\\.[0-9]+)?% \\(([0-9]+)/([0-9]+)\\)$", RegexOptions.CultureInvariant)]
    private static partial Regex ConditionPattern();
}
