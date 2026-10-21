using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace ViajantesTurismo.RepoConfig.Tool;

internal static class CiTestSliceCommand
{
    public static async Task<int> Run(string[] args, TextWriter output, string root, CancellationToken ct)
    {
        string? name = null, projectsFile = null;
        var playwright = false;
        var projects = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--install-playwright") { playwright = true; continue; }
            if (args[index] == "--") { projects.AddRange(args[(index + 1)..]); break; }
            if (args[index] is "--slice-name" or "--projects-file")
            {
                if (index + 1 == args.Length) { throw new ArgumentException("Missing slice option value."); }
                if (args[index] == "--slice-name") { name = args[++index]; } else { projectsFile = args[++index]; }
            }
            else if (args[index].StartsWith("--", StringComparison.Ordinal)) { throw new ArgumentException("Unknown slice option."); }
            else { projects.Add(args[index]); }
        }
        if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("--slice-name is required."); }
        if (projectsFile is not null)
        {
            if (projects.Count > 0) { throw new ArgumentException("Do not combine a project manifest with positional projects."); }
            projects.AddRange(await File.ReadAllLinesAsync(Path.GetFullPath(projectsFile, root), ct).ConfigureAwait(false));
        }
        projects = projects.Select(path => path.Trim().Replace('\\', '/')).Where(path => path.Length > 0).ToList();
        if (projects.Count == 0 || projects.Distinct(StringComparer.Ordinal).Count() != projects.Count)
        {
            throw new ArgumentException("A slice requires distinct test projects.");
        }
        var solutionProjects = XDocument.Load(Path.Combine(root, "ViajantesTurismo.slnx")).Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value?.Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
        if (projects.Any(path => !solutionProjects.Contains(path) || !path.StartsWith("tests/", StringComparison.Ordinal)
            || path.Split('/').Contains("..", StringComparer.Ordinal) || !File.Exists(Path.Combine(root, path))))
        {
            throw new ArgumentException("A slice can execute only test projects in the repository solution.");
        }
        var slug = new string(name.Select(character => char.IsAsciiLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-').ToArray());
        if (slug.All(character => character == '-')) { slug = "slice"; }
        var results = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(results);
        var revision = await CandidateCompatibilityCommand.RunProcess("git", ["rev-parse", "HEAD"], root, TextWriter.Null, ct).ConfigureAwait(false);
        var plan = CiProjectGraph.Write(root, results, slug + "-build", projects);
        await File.WriteAllLinesAsync(Path.Combine(results, slug + "-projects.txt"), projects, ct).ConfigureAwait(false);
        var timings = Path.Combine(results, slug + "-phase-timings.tsv");
        await File.WriteAllTextAsync(timings, "phase\tdescription\tstarted_at_utc\tcompleted_at_utc\tduration_seconds\tstatus\tlog_file\n", ct).ConfigureAwait(false);
        var phases = new List<(string Name, string Status, double Seconds, string Log)>();
        var started = DateTimeOffset.UtcNow;
        var status = "failed";
        try
        {
            await Phase("build_projects", "dotnet", ["build", plan, "--no-restore"], slug + "-build.log", root, output, timings, phases, ct).ConfigureAwait(false);
            foreach (var service in projects.Select(path => path switch
            {
                "tests/ViajantesTurismo.Admin.ContractTests/ViajantesTurismo.Admin.ContractTests.csproj" => "admin",
                "tests/ViajantesTurismo.Branding.ContractTests/ViajantesTurismo.Branding.ContractTests.csproj" => "branding",
                "tests/ViajantesTurismo.Catalog.ContractTests/ViajantesTurismo.Catalog.ContractTests.csproj" => "catalog",
                _ => null
            }).OfType<string>())
            {
                await Phase("prepare_openapi_artifacts", "dotnet", ["run", "--project", "tools/ViajantesTurismo.OpenApi.Tool", "--no-restore", "--", "generate", service],
                    slug + "-openapi-" + service + ".log", root, output, timings, phases, ct).ConfigureAwait(false);
            }
            if (playwright)
            {
                var script = projects.Select(path => Path.GetDirectoryName(Path.Combine(root, path)) ?? root)
                    .SelectMany(directory => Directory.EnumerateFiles(directory, "playwright.ps1", SearchOption.AllDirectories))
                    .FirstOrDefault(path => path.Replace('\\', '/').Contains("/bin/Debug/", StringComparison.Ordinal))
                    ?? throw new InvalidOperationException("Playwright install script was not produced by the slice build.");
                await Phase("install_playwright", "bash", ["scripts/install-playwright.sh", script, "chromium"], slug + "-playwright-install.log",
                    root, output, timings, phases, ct).ConfigureAwait(false);
            }
            await Phase("collect_test_coverage", "bash", ["scripts/collect-test-coverage.sh", "TestResults/" + slug + "-coverage-reports.txt", .. projects],
                slug + "-coverage-collection.log", root, output, timings, phases, ct).ConfigureAwait(false);
            status = "success";
            return 0;
        }
        finally
        {
            await File.AppendAllTextAsync(timings, $"total_slice\t{slug}\t{started:O}\t{DateTimeOffset.UtcNow:O}\t{(DateTimeOffset.UtcNow - started).TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}\t{status}\t-\n",
                CancellationToken.None).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteString("job_type", "test_slice");
                writer.WriteString("slice_name", name);
                writer.WriteString("slice_slug", slug);
                writer.WriteString("revision", revision);
                writer.WriteString("status", status);
                writer.WriteString("workflow_started_at_utc", started);
                writer.WriteString("completed_at_utc", DateTimeOffset.UtcNow);
                writer.WriteBoolean("install_playwright", playwright);
                writer.WriteStartObject("files");
                writer.WriteString("timing_file", "TestResults/" + slug + "-phase-timings.tsv");
                writer.WriteString("build_log", "TestResults/" + slug + "-build.log");
                writer.WriteString("coverage_collection_log", "TestResults/" + slug + "-coverage-collection.log");
                if (playwright) { writer.WriteString("playwright_install_log", "TestResults/" + slug + "-playwright-install.log"); }
                else { writer.WriteNull("playwright_install_log"); }
                writer.WriteEndObject();
                writer.WriteStartArray("projects");
                foreach (var project in projects) { writer.WriteStringValue(project); }
                writer.WriteEndArray();
                writer.WriteStartArray("phases");
                foreach (var phase in phases)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", phase.Name);
                    writer.WriteString("status", phase.Status);
                    writer.WriteNumber("duration_seconds", phase.Seconds);
                    writer.WriteString("log_file", phase.Log);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            await File.WriteAllBytesAsync(Path.Combine(results, slug + "-manifest.json"), buffer.ToArray(), CancellationToken.None).ConfigureAwait(false);
            var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrEmpty(summary))
            {
                await File.AppendAllTextAsync(summary, $"## {slug}\n\n" + string.Join('\n', phases.Select(phase =>
                    $"- {phase.Name}: {phase.Status}, {phase.Seconds.ToString("F1", CultureInfo.InvariantCulture)}s")) + "\n", CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private static async Task Phase(string phase, string executable, string[] args, string log, string root, TextWriter output,
        string timings, List<(string Name, string Status, double Seconds, string Log)> phases, CancellationToken ct)
    {
        var started = DateTimeOffset.UtcNow;
        var timer = Stopwatch.StartNew();
        var status = "failed";
        using var captured = new StringWriter(CultureInfo.InvariantCulture);
        try
        {
            await output.WriteLineAsync("Starting " + phase).ConfigureAwait(false);
            await CandidateCompatibilityCommand.RunProcess(executable, args, root, captured, ct).ConfigureAwait(false);
            status = "success";
        }
        catch (Exception exception) when (exception is InvalidOperationException or OperationCanceledException)
        {
            await captured.WriteLineAsync(exception.Message).ConfigureAwait(false);
            throw;
        }
        finally
        {
            var relativeLog = "TestResults/" + log;
            await File.WriteAllTextAsync(Path.Combine(root, relativeLog), captured.ToString(), CancellationToken.None).ConfigureAwait(false);
            await output.WriteAsync(captured.ToString()).ConfigureAwait(false);
            phases.Add((phase, status, timer.Elapsed.TotalSeconds, relativeLog));
            await File.AppendAllTextAsync(timings, $"{phase}\t{phase}\t{started:O}\t{DateTimeOffset.UtcNow:O}\t{timer.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)}\t{status}\t{relativeLog}\n",
                CancellationToken.None).ConfigureAwait(false);
        }
    }
}
