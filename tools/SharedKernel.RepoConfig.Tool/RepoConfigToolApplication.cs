using System.Globalization;
using System.Text.Json;

namespace SharedKernel.RepoConfig.Tool;

internal static class RepoConfigToolApplication
{
    private const string Usage = "Usage: sharedkernel-repo <init|verify|diff|set|get|sync> [--root <path>]";

    public static int Run(string[] args, TextWriter output, TextWriter error, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        if (args is [] or ["--help"] or ["-h"])
        {
            output.WriteLine(Usage);
            output.WriteLine("Commands: init, verify, diff, set github.repository <owner/repo>, get <query>, sync github.");
            return 0;
        }

        try
        {
            return RunCommand(args, output, error, workingDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException
            or IOException
            or JsonException
            or NotSupportedException
            or UnauthorizedAccessException
            or InvalidOperationException)
        {
            error.WriteLine($"repo-config: {exception.Message}");
            return 1;
        }
    }

    private static int RunCommand(string[] args, TextWriter output, TextWriter error, string workingDirectory)
    {
        var command = args[0];
        return command switch
        {
            "init" => RunInit(args[1..], output, error, workingDirectory),
            "verify" => RunVerify(args[1..], output, error, workingDirectory),
            "diff" => RunDiff(args[1..], output, error, workingDirectory),
            "set" => RunSet(args[1..], output, error, workingDirectory),
            "get" => RunGet(args[1..], output, error, workingDirectory),
            "sync" => RunSync(args[1..], output, error, workingDirectory),
            _ => WriteUsageError(error, $"Unknown command: {command}")
        };
    }

    private static int RunInit(string[] args, TextWriter output, TextWriter error, string workingDirectory)
    {
        if (!TryParseRootOption(args, workingDirectory, error, out var rootPath, out var remaining) || remaining.Length > 0)
        {
            return 2;
        }

        var createdPaths = RepoConfigInitializer.Initialize(rootPath);
        if (createdPaths.Count == 0)
        {
            output.WriteLine("Repository roadmap structure is already initialized.");
            return 0;
        }

        output.WriteLine("Initialized repository roadmap structure:");
        foreach (var path in createdPaths)
        {
            output.WriteLine($"- {path}");
        }

        return 0;
    }

    private static int RunVerify(string[] args, TextWriter output, TextWriter error, string workingDirectory)
    {
        if (!TryParseRootOption(args, workingDirectory, error, out var rootPath, out var remaining) || remaining.Length > 0)
        {
            return 2;
        }

        var issues = RepoConfigVerifier.Verify(rootPath);
        if (issues.Count == 0)
        {
            output.WriteLine("Repository config is valid.");
            return 0;
        }

        WriteIssues(error, "Repository config verification failed:", issues);
        return 1;
    }

    private static int RunDiff(string[] args, TextWriter output, TextWriter error, string workingDirectory)
    {
        if (!TryParseRootOption(args, workingDirectory, error, out var rootPath, out var remaining) || remaining.Length > 0)
        {
            return 2;
        }

        var issues = RepoConfigVerifier.Verify(rootPath);
        if (issues.Count == 0)
        {
            output.WriteLine("Repository config has no drift.");
            return 0;
        }

        WriteIssues(error, "Repository config drift:", issues);
        return 1;
    }

    private static int RunSet(string[] args, TextWriter output, TextWriter error, string workingDirectory)
    {
        if (!TryParseRootOption(args, workingDirectory, error, out var rootPath, out var remaining))
        {
            return 2;
        }

        if (remaining.Length < 2)
        {
            return WriteUsageError(error, "Missing set key and value.");
        }

        if (remaining.Length > 2)
        {
            return WriteUsageError(error, $"Unknown argument: {remaining[2]}");
        }

        var key = remaining[0];
        var value = remaining[1];

        RepoConfigSetter.Set(rootPath, key, value);
        output.WriteLine($"Updated {key}.");
        return 0;
    }

    private static int RunGet(string[] args, TextWriter output, TextWriter error, string workingDirectory)
    {
        if (!TryParseRootOption(args, workingDirectory, error, out var rootPath, out var remaining))
        {
            return 2;
        }

        if (remaining.Length == 0)
        {
            return WriteUsageError(error, "Missing get query.");
        }

        var project = RoadmapProject.Load(rootPath);
        var query = remaining[0];
        if (!TryParseGetOptions(remaining[1..], error, out var positionalArgs, out var type, out var limit))
        {
            return 2;
        }

        switch (query)
        {
            case "next-priority":
                WriteItems(output, project.OpenItems(type).OrderByPriority().Take(limit));
                return 0;

            case "next-unblocked":
                WriteItems(output, project.OpenItems(type).Where(project.IsUnblocked).OrderByPriority().Take(limit));
                return 0;

            case "blockers-of" when positionalArgs is [var itemId]:
                WriteItems(output, project.BlockersOf(itemId).OrderByPriority().Take(limit));
                return 0;

            case "next-blockers":
                WriteItems(output, project.OpenItems(type).Where(item => item.Type == "blocker" || item.Blocks.Count > 0).OrderByDescending(item => item.Blocks.Count).ThenBy(item => item.Order).Take(limit));
                return 0;

            case "next-enablers":
                WriteItems(output, project.OpenItems("enabler").Where(project.IsUnblocked).OrderByPriority().Take(limit));
                return 0;

            case "low-hanging-fruit":
                WriteItems(output, project.OpenItems(type).Where(project.IsUnblocked).OrderBy(item => item.Effort).ThenByDescending(item => item.Score).ThenBy(item => item.Order).Take(limit));
                return 0;

            case "pareto":
                var paretoLimit = Math.Max(1, (int)Math.Ceiling(project.OpenItems(type).Count * 0.2m));
                WriteItems(output, project.OpenItems(type).Where(project.IsUnblocked).OrderByDescending(item => item.Score).ThenBy(item => item.Effort).Take(Math.Min(limit, paretoLimit)));
                return 0;

            case "blocking-overview":
                WriteBlockingOverview(output, project.OpenItems(type).OrderByPriority(), project);
                return 0;

            case "tags":
                WriteCounts(output, project.TagCounts());
                return 0;

            case "labels":
                WriteCounts(output, project.LabelCounts());
                return 0;

            case "by-tag" when positionalArgs is [var tag]:
                WriteItems(output, project.OpenItems(type).Where(item => item.Tags.Contains(tag, StringComparer.Ordinal)).OrderByPriority().Take(limit));
                return 0;

            case "by-label" when positionalArgs is [var label]:
                WriteItems(output, project.OpenItems(type).Where(item => item.Labels.Contains(label, StringComparer.Ordinal)).OrderByPriority().Take(limit));
                return 0;

            default:
                return WriteUsageError(error, $"Unknown or incomplete get query: {query}");
        }
    }

    private static int RunSync(string[] args, TextWriter output, TextWriter error, string workingDirectory)
    {
        if (!TryParseRootOption(args, workingDirectory, error, out var rootPath, out var remaining))
        {
            return 2;
        }

        if (remaining.Length == 0 || !string.Equals(remaining[0], "github", StringComparison.Ordinal))
        {
            return WriteUsageError(error, "Missing sync target: github.");
        }

        var dryRun = !remaining.Contains("--apply", StringComparer.Ordinal);
        if (remaining.Contains("--dry-run", StringComparer.Ordinal) && remaining.Contains("--apply", StringComparer.Ordinal))
        {
            return WriteUsageError(error, "Use either --dry-run or --apply, not both.");
        }

        if (remaining.Any(argument => argument is not "github" and not "--dry-run" and not "--apply"))
        {
            return WriteUsageError(error, "Unknown sync argument.");
        }

        var project = RoadmapProject.Load(rootPath);
        var syncer = new GitHubRoadmapSyncer(project);
        var result = syncer.Sync(dryRun);
        foreach (var message in result.Messages)
        {
            output.WriteLine(message);
        }

        return 0;
    }

    private static bool TryParseRootOption(string[] args, string workingDirectory, TextWriter error, out string rootPath, out string[] remaining)
    {
        rootPath = Path.GetFullPath(workingDirectory);
        List<string> remainingArgs = [];
        var index = 0;
        while (index < args.Length)
        {
            if (args[index] == "--root")
            {
                if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
                {
                    remaining = [];
                    WriteUsageError(error, "Missing required value for --root.");
                    return false;
                }

                rootPath = Path.GetFullPath(args[index + 1], workingDirectory);
                index += 2;
                continue;
            }

            remainingArgs.Add(args[index]);
            index++;
        }

        remaining = [.. remainingArgs];
        return true;
    }

    private static int WriteUsageError(TextWriter error, string message)
    {
        error.WriteLine(message);
        error.WriteLine(Usage);
        return 2;
    }

    private static void WriteIssues(TextWriter error, string heading, IReadOnlyCollection<RepoConfigIssue> issues)
    {
        error.WriteLine(heading);
        foreach (var issue in issues)
        {
            error.WriteLine($"- {issue.Path}: {issue.Message}");
        }
    }

    private static bool TryParseGetOptions(string[] args, TextWriter error, out string[] positionalArgs, out string? type, out int limit)
    {
        List<string> positionalValues = [];
        type = null;
        limit = 10;
        var index = 0;
        while (index < args.Length)
        {
            switch (args[index])
            {
                case "--type" when HasOptionValue(args, index):
                    type = args[index + 1];
                    index += 2;
                    continue;

                case "--type":
                    WriteUsageError(error, "Missing required value for --type.");
                    positionalArgs = [];
                    return false;

                case "--limit" when HasOptionValue(args, index) && int.TryParse(args[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedLimit) && parsedLimit > 0:
                    limit = parsedLimit;
                    index += 2;
                    continue;

                case "--limit":
                    WriteUsageError(error, "Missing or invalid required value for --limit.");
                    positionalArgs = [];
                    return false;

                default:
                    if (args[index].StartsWith("--", StringComparison.Ordinal))
                    {
                        WriteUsageError(error, $"Unknown get option: {args[index]}");
                        positionalArgs = [];
                        return false;
                    }

                    positionalValues.Add(args[index]);
                    index++;
                    continue;
            }
        }

        positionalArgs = [.. positionalValues];
        return true;
    }

    private static bool HasOptionValue(string[] args, int index) =>
        index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal);

    private static void WriteItems(TextWriter output, IEnumerable<RoadmapItemSnapshot> items)
    {
        var count = 0;
        foreach (var item in items)
        {
            output.WriteLine($"{item.Id} | {item.Type} | {item.Status} | order {item.Order.ToString(CultureInfo.InvariantCulture)} | score {item.Score.ToString("0.##", CultureInfo.InvariantCulture)} | {item.Title}");
            count++;
        }

        if (count == 0)
        {
            output.WriteLine("No matching roadmap items.");
        }
    }

    private static void WriteCounts(TextWriter output, IEnumerable<KeyValuePair<string, int>> counts)
    {
        var count = 0;
        foreach (var item in counts)
        {
            output.WriteLine($"{item.Key} | {item.Value.ToString(CultureInfo.InvariantCulture)}");
            count++;
        }

        if (count == 0)
        {
            output.WriteLine("No matching values.");
        }
    }

    private static void WriteBlockingOverview(TextWriter output, IEnumerable<RoadmapItemSnapshot> items, RoadmapProject project)
    {
        var count = 0;
        var blockedItems = items
            .Select(item => new
            {
                Item = item,
                Blockers = project.BlockersOf(item.Id).Where(blocker => !project.IsClosed(blocker)).Select(blocker => blocker.Id).ToArray()
            })
            .Where(item => item.Blockers.Length > 0);

        foreach (var item in blockedItems)
        {
            output.WriteLine($"{item.Item.Id} blocked by {string.Join(", ", item.Blockers)}");
            count++;
        }

        if (count == 0)
        {
            output.WriteLine("No blocked roadmap items.");
        }
    }
}
