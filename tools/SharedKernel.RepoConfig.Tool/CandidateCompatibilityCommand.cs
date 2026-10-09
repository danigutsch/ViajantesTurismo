using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace SharedKernel.RepoConfig.Tool;

internal static class CandidateCompatibilityCommand
{
    private static readonly string NuGetSource = string.Concat("https://api.nuget.org", "/v3/index.json");

    public static async Task<int> Run(string[] args, TextWriter output, TextWriter error, string workingDirectory, CancellationToken ct)
    {
        var root = workingDirectory;
        string? candidate = null;
        var destination = "TestResults/consumer-compatibility.json";
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 == args.Length)
            {
                throw new ArgumentException("Missing value for " + args[index]);
            }

            switch (args[index])
            {
                case "--root": root = Path.GetFullPath(args[index + 1]); break;
                case "--candidate": candidate = Path.GetFullPath(args[index + 1]); break;
                case "--output": destination = args[index + 1]; break;
                default: throw new ArgumentException("Unknown option: " + args[index]);
            }
        }

        if (candidate is null)
        {
            throw new ArgumentException("--candidate is required.");
        }

        destination = Path.GetFullPath(destination, root);
        var evidence = JsonNode.Parse(await File.ReadAllTextAsync(candidate, ct).ConfigureAwait(false))!.AsObject();
        evidence["consumerRevision"] = await RunProcess("git", ["rev-parse", "HEAD"], root, TextWriter.Null, ct).ConfigureAwait(false);
        evidence["consumerDirty"] = (await RunProcess("git", ["status", "--porcelain"], root, TextWriter.Null, ct).ConfigureAwait(false)).Length != 0;
        var status = "pending";
        try
        {
            if (evidence["libraryDirty"]!.GetValue<bool>() || evidence["consumerDirty"]!.GetValue<bool>())
            {
                throw new ArgumentException("Compatibility requires committed library and consumer revisions.");
            }

            ValidateConsumer(root, candidate, evidence);
            var scratch = Path.Combine(Path.GetTempPath(), "sharedkernel-consumer-" + Guid.NewGuid().ToString("N"));
            var config = WriteRestoreConfiguration(candidate, scratch);
            var cache = Path.Combine(scratch, "packages");
            status = "failed";
            string[][] commands = [
                ["restore", "ViajantesTurismo.slnx", "--locked-mode", "--configfile", config, "--packages", cache, "--no-cache"],
                ["build", "ViajantesTurismo.slnx", "-c", "Release", "--no-restore"],
                ["run", "--project", "tools/ViajantesTurismo.OpenApi.Tool", "-c", "Release", "--no-build", "--no-restore", "--", "generate", "all"],
                ["test", "--solution", "ViajantesTurismo.slnx", "-c", "Release", "--no-build", "--no-restore"]
            ];
            foreach (var command in commands)
            {
                await RunProcess("dotnet", command, root, output, ct).ConfigureAwait(false);
            }

            status = "passed";
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException)
        {
            evidence["reason"] = exception.Message;
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
        }

        evidence["consumerCompatibility"] = status;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await File.WriteAllTextAsync(destination, evidence.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine, ct).ConfigureAwait(false);
        await output.WriteLineAsync(evidence["consumerCompatibility"]!.GetValue<string>()).ConfigureAwait(false);
        return evidence["consumerCompatibility"]!.GetValue<string>() == "passed" ? 0 : 1;
    }

    internal static string WriteRestoreConfiguration(string candidate, string scratch)
    {
        Directory.CreateDirectory(scratch);
        var path = Path.Combine(scratch, "NuGet.config");
        new XDocument(new XElement("configuration",
            new XElement("packageSources", new XElement("clear"),
                new XElement("add", new XAttribute("key", "local"), new XAttribute("value", Path.GetDirectoryName(Path.GetFullPath(candidate))!)),
                new XElement("add", new XAttribute("key", "nuget.org"), new XAttribute("value", NuGetSource))),
            new XElement("auditSources", new XElement("clear"), new XElement("add", new XAttribute("key", "nuget.org"), new XAttribute("value", NuGetSource))),
            new XElement("packageSourceMapping",
                new XElement("packageSource", new XAttribute("key", "local"), new XElement("package", new XAttribute("pattern", "SharedKernel.*"))),
                new XElement("packageSource", new XAttribute("key", "nuget.org"), new XElement("package", new XAttribute("pattern", "*")))),
            new XElement("fallbackPackageFolders", new XElement("clear")))).Save(path);
        return path;
    }

    internal static void ValidateConsumer(string root, string candidate, JsonObject evidence)
    {
        var hashes = evidence["packageSha256"]?.AsObject() ?? throw new ArgumentException("Candidate package hashes are unavailable.");
        if (hashes.Count == 0)
        {
            throw new ArgumentException("Candidate package hashes are unavailable.");
        }

        foreach (var (name, hash) in hashes)
        {
            if (name.Contains('/', StringComparison.Ordinal) || name.Contains('\\', StringComparison.Ordinal) || Path.GetFileName(name) != name || !name.EndsWith(".nupkg", StringComparison.Ordinal))
            {
                throw new ArgumentException("Invalid candidate package filename: " + name);
            }

            using var stream = File.OpenRead(Path.Combine(Path.GetDirectoryName(candidate)!, name));
            if (Convert.ToHexStringLower(SHA256.HashData(stream)) != hash!.GetValue<string>())
            {
                throw new ArgumentException("Candidate package changed: " + name);
            }
        }

        var version = evidence["packageVersion"]!.GetValue<string>();
        var central = XDocument.Load(Path.Combine(root, "Directory.Packages.props")).Descendants("PackageVersion")
            .Where(x => ((string?)x.Attribute("Include"))?.StartsWith("SharedKernel.", StringComparison.Ordinal) == true).ToArray();
        if (central.Length == 0 || central.Any(x => (string?)x.Attribute("Version") != version && (string?)x.Attribute("Version") != "[" + version + "]"))
        {
            throw new ArgumentException("Consumer must declare the tested candidate minimum version.");
        }

        foreach (var project in XDocument.Load(Path.Combine(root, "ViajantesTurismo.slnx")).Descendants("Project"))
        {
            var path = Path.Combine(root, (string)project.Attribute("Path")!);
            foreach (var reference in XDocument.Load(path).Descendants("ProjectReference"))
            {
                var id = Path.GetFileNameWithoutExtension(((string?)reference.Attribute("Include") ?? string.Empty).Replace('\\', '/'));
                if (id.StartsWith("SharedKernel.", StringComparison.Ordinal) && id is not "SharedKernel.Documentation" && File.Exists(Path.Combine(root, "src/SharedKernel", id, id + ".csproj")))
                {
                    throw new ArgumentException("Consumer still references extracted SharedKernel sources: " + path);
                }
            }

            var lockPath = Path.Combine(Path.GetDirectoryName(path)!, "packages.lock.json");
            var dependencies = JsonNode.Parse(File.ReadAllText(lockPath))!["dependencies"]!.AsObject();
            foreach (var (_, framework) in dependencies)
            {
                foreach (var (id, value) in framework!.AsObject())
                {
                    if (id.StartsWith("SharedKernel.", StringComparison.Ordinal) && id is not "SharedKernel.Documentation"
                        && value!["type"]!.GetValue<string>() == "Project"
                        && File.Exists(Path.Combine(root, "src/SharedKernel", id, id + ".csproj")))
                    {
                        throw new ArgumentException("Consumer still references extracted SharedKernel sources: " + lockPath);
                    }

                    if (id.StartsWith("SharedKernel.", StringComparison.Ordinal) && value!["type"]!.GetValue<string>() != "Project" && value["resolved"]!.GetValue<string>() != version)
                    {
                        throw new ArgumentException(lockPath + ": " + id + " does not resolve to the tested candidate.");
                    }
                }
            }
        }
    }

    private static async Task<string> RunProcess(string name, string[] args, string root, TextWriter output, CancellationToken ct)
    {
        var info = new ProcessStartInfo(name) { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in args) { info.ArgumentList.Add(argument); }
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start " + name);
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        var text = await stdout.ConfigureAwait(false);
        var errors = await stderr.ConfigureAwait(false);
        await output.WriteAsync(text).ConfigureAwait(false);
        if (process.ExitCode != 0) { throw new InvalidOperationException($"{name} exited {process.ExitCode}: {text}{Environment.NewLine}{errors}"); }
        if (!string.IsNullOrEmpty(errors)) { await output.WriteAsync(errors).ConfigureAwait(false); }
        return text.Trim();
    }
}
