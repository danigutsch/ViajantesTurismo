using System.Security.Cryptography;
using System.Text.Json;

namespace ViajantesTurismo.RepoConfig.Tool;

internal static class BuildEvidenceCommand
{
    private const string Manifest = "TestResults/build-evidence.json";
    private static readonly string[] SourceDirectories = ["src", "tools"];

    public static async Task<int> Run(bool write, string[] args, string root, CancellationToken ct)
    {
        if (args.Length != 0) { throw new ArgumentException("Build evidence commands take no arguments."); }
        var revision = await CandidateCompatibilityCommand.RunProcess("git", ["rev-parse", "HEAD"], root, TextWriter.Null, ct).ConfigureAwait(false);
        var manifest = Path.Combine(root, Manifest);
        if (write)
        {
            var files = SourceDirectories.Select(directory => Path.Combine(root, directory))
                .Where(Directory.Exists).SelectMany(directory => Directory.EnumerateFiles(directory, "*.pdb", SearchOption.AllDirectories))
                .Where(filename => filename.Replace('\\', '/').Contains("/bin/Release/", StringComparison.Ordinal))
                .Select(filename => Path.GetRelativePath(root, filename).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
            if (files.Length == 0) { throw new InvalidOperationException("No Release symbols were produced by the solution build."); }
            Directory.CreateDirectory(Path.GetDirectoryName(manifest) ?? root);
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteString("revision", revision);
                writer.WriteStartObject("files");
                foreach (var file in files) { writer.WriteString(file, await Hash(root, file, ct).ConfigureAwait(false)); }
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            await File.WriteAllBytesAsync(manifest, buffer.ToArray(), ct).ConfigureAwait(false);
        }
        else
        {
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(manifest, ct).ConfigureAwait(false));
            if (document.RootElement.GetProperty("revision").GetString() != revision)
            {
                throw new InvalidOperationException("Build evidence does not match the tested revision.");
            }
            var files = document.RootElement.GetProperty("files");
            if (files.ValueKind != JsonValueKind.Object || !files.EnumerateObject().Any())
            {
                throw new InvalidOperationException("Build evidence contains no symbols.");
            }
            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in files.EnumerateObject())
            {
                if (!declared.Add(file.Name) || file.Value.GetString() != await Hash(root, file.Name, ct).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Build symbols do not match their evidence.");
                }
            }
            // Undeclared symbols must not be accepted by the coverage checker's directory scan.
            foreach (var directory in SourceDirectories.Select(path => Path.Combine(root, path)).Where(Directory.Exists))
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*.pdb", SearchOption.AllDirectories)
                    .Where(path => path.Replace('\\', '/').Contains("/bin/Release/", StringComparison.Ordinal)))
                {
                    if (!declared.Contains(Path.GetRelativePath(root, file).Replace('\\', '/')))
                    {
                        throw new InvalidOperationException("Undeclared Release symbols were found in the coverage workspace.");
                    }
                }
            }
        }
        return 0;
    }

    private static async Task<string> Hash(string root, string path, CancellationToken ct)
    {
        if (Path.IsPathRooted(path) || path.Contains('\\', StringComparison.Ordinal) || !path.EndsWith(".pdb", StringComparison.Ordinal)
            || !(path.StartsWith("src/", StringComparison.Ordinal) || path.StartsWith("tools/", StringComparison.Ordinal))
            || !path.Contains("/bin/Release/", StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid build evidence path.");
        }
        var full = Path.GetFullPath(path, root);
        var relative = Path.GetRelativePath(root, full);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)) { throw new ArgumentException("Build evidence escapes the checkout."); }
        for (var item = new FileInfo(full) as FileSystemInfo; item is not null && item.FullName != Path.GetFullPath(root); item = item is FileInfo file ? file.Directory : ((DirectoryInfo)item).Parent)
        {
            if (item.LinkTarget is not null) { throw new ArgumentException("Build evidence cannot traverse symbolic links."); }
        }
        using var stream = File.OpenRead(full);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false));
    }
}
