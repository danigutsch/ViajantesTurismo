using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static partial class SharedKernelToolCallerTestsHelpers
{
    private const string Placeholder = "placeholder";

    // The CI and RepoConfig tools also run from SharedKernel source; the packaged tool of the pinned version parses those lines.
    private static readonly Dictionary<string, string> SourceToolCommands = new(StringComparer.Ordinal)
    {
        ["SharedKernel.CI.Tool"] = "sharedkernel-ci",
        ["SharedKernel.RepoConfig.Tool"] = "sharedkernel-repo",
    };

    // Shell arrays that a script fills at run time, mapped to every argument list the script can build.
    private static readonly Dictionary<string, string[][]> ArrayArguments = new(StringComparer.Ordinal)
    {
        ["@source"] = [["--feed", Placeholder], ["--repository", "danigutsch/PandaBear.SharedKernel"]],
        ["@stageSource"] = [["--feed", Placeholder], ["--repository", "danigutsch/PandaBear.SharedKernel"]],
        ["\"${stage_source[@]}\""] = [["--feed", Placeholder], ["--repository", "danigutsch/PandaBear.SharedKernel"]],
    };

    // Shell variables whose value an option restricts, mapped to every value a caller can pass; others become a placeholder.
    private static readonly Dictionary<string, string[]> VariableValues = new(StringComparer.Ordinal)
    {
        ["GITHUB_REPOSITORY"] = ["owner/name"],
        ["RELEASE_VERSION_KIND"] = ["prerelease", "stable"],
    };

    private static readonly string[] TextCallerPatterns =
    [
        ".github/workflows/*.yml",
        ".github/actions/*/action.yml",
        ".devcontainer/*.sh",
        "scripts/*.sh",
        "scripts/*.ps1",
        "setup-dev.sh",
        "setup-dev.ps1",
        "tests/*.ContractTests/*.cs",
    ];

    private static readonly string[] JsonCallerPatterns =
    [
        "opencode.json",
        ".vscode/*.json",
        "*.code-workspace",
    ];

    private static readonly JsonDocumentOptions JsonCOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// sharedkernel-codefix has no [diagram] directive, so its command lines cannot be parsed without running it.
    /// </summary>
    public const string CommandWithoutDiagramDirective = "sharedkernel-codefix";

    public static IReadOnlyList<ToolCommandLine> FindCommandLines(string root)
    {
        List<ToolCommandLine> lines = [];
        foreach (var path in ExpandPatterns(root, TextCallerPatterns))
        {
            var text = File.ReadAllText(Path.Combine(root, path));
            foreach (var line in JoinContinuations(text))
            {
                lines.AddRange(ParseLine(path, line));
            }
        }

        foreach (var path in ExpandPatterns(root, JsonCallerPatterns))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, path)), JsonCOptions);
            foreach (var line in FindJsonCommands(document.RootElement))
            {
                lines.AddRange(ParseLine(path, line));
            }
        }

        return lines;
    }

    public static async Task<string?> ParseWithDiagramDirective(string root, ToolCommandLine line, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in (string[])["tool", "run", line.Command, "--", "[diagram]", .. line.Arguments])
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("dotnet did not start.");
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        // The directive prints the parse tree without running the command and returns 1 when the parse has errors.
        return process.ExitCode == 0
            ? null
            : $"{line.Source}: {line.Command} {string.Join(' ', line.Arguments)} exited {process.ExitCode}: {await output}{await error}";
    }

    private static IEnumerable<string> ExpandPatterns(string root, string[] patterns) =>
        patterns
            .SelectMany(pattern =>
            {
                var directory = Path.GetDirectoryName(pattern) ?? string.Empty;
                var segments = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
                IEnumerable<string> directories = [root];
                foreach (var segment in segments)
                {
                    directories = directories.SelectMany(parent => Directory.Exists(parent)
                        ? Directory.EnumerateDirectories(parent, segment)
                        : []);
                }

                return directories.SelectMany(parent => Directory.EnumerateFiles(parent, Path.GetFileName(pattern)));
            })
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

    private static IEnumerable<string> JoinContinuations(string text)
    {
        StringBuilder current = new();
        foreach (var rawLine in text.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = current.Length == 0 ? rawLine : rawLine.TrimStart();
            var trimmed = line.TrimEnd();

            // Bash continues a line with a trailing backslash and PowerShell with a trailing backtick.
            if (trimmed.EndsWith('\\') || trimmed.EndsWith('`'))
            {
                current.Append(trimmed[..^1]).Append(' ');
                continue;
            }

            current.Append(line);
            yield return current.ToString();
            current.Clear();
        }
    }

    private static IEnumerable<string> FindJsonCommands(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray().SelectMany(FindJsonCommands))
            {
                yield return child;
            }

            yield break;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        // opencode.json holds the command line in a "command" array; VS Code tasks and MCP servers split it into "command" and "args".
        if (element.TryGetProperty("command", out var command))
        {
            IEnumerable<JsonElement> tokens = command.ValueKind == JsonValueKind.Array ? command.EnumerateArray() : [command];
            if (element.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Array)
            {
                tokens = tokens.Concat(args.EnumerateArray());
            }

            if (tokens.All(static token => token.ValueKind == JsonValueKind.String))
            {
                yield return string.Join(' ', tokens.Select(static token => Quote(token.GetString()!)));
            }
        }

        // opencode.json command templates run their shell text inside !`...`.
        if (element.TryGetProperty("template", out var template) && template.ValueKind == JsonValueKind.String)
        {
            yield return template.GetString()!;
        }

        foreach (var child in element.EnumerateObject().SelectMany(property => FindJsonCommands(property.Value)))
        {
            yield return child;
        }
    }

    private static IEnumerable<ToolCommandLine> ParseLine(string source, string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith('#') || trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            yield break;
        }

        foreach (Match match in ToolCommandRegex().Matches(line))
        {
            var command = match.Groups["command"].Success
                ? match.Groups["command"].Value
                : SourceToolCommands.GetValueOrDefault(match.Groups["project"].Value, match.Groups["project"].Value);
            var arguments = line[(match.Index + match.Length)..];

            // A command line quoted inside a hint string ends with that string.
            if (line[..match.Index].Count(static character => character == '"') % 2 == 1)
            {
                arguments = arguments.Split('"')[0].Replace("\\n", string.Empty, StringComparison.Ordinal);
            }

            foreach (var expanded in ExpandArrays(Tokenize(arguments)))
            {
                yield return new ToolCommandLine(source, command, expanded);
            }
        }
    }

    private static IEnumerable<string[]> ExpandArrays(IReadOnlyList<string> tokens)
    {
        IEnumerable<string[]> lists = [[]];
        foreach (var token in tokens)
        {
            if (ArrayArguments.TryGetValue(token, out var alternatives))
            {
                lists = lists.SelectMany(list => alternatives.Select(alternative => (string[])[.. list, .. alternative]));
            }
            else if (token.StartsWith('@') || token.Contains("[@]", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Add the shell array {token} to {nameof(ArrayArguments)} with every value it can hold.");
            }
            else
            {
                var value = Unquote(token);
                string[] values = value.Contains('$', StringComparison.Ordinal)
                    ? VariableValues.GetValueOrDefault(VariableNameRegex().Match(value).Groups["name"].Value, [Placeholder])
                    : [value];
                lists = lists.SelectMany(list => values.Select(alternative => (string[])[.. list, alternative]));
            }
        }

        return lists;
    }

    private static List<string> Tokenize(string arguments)
    {
        arguments = WorkflowExpressionRegex().Replace(arguments, Placeholder);
        List<string> tokens = [];
        StringBuilder current = new();
        char? quote = null;
        foreach (var character in arguments)
        {
            if (quote is not null)
            {
                current.Append(character);
                if (character == quote)
                {
                    quote = null;
                }

                continue;
            }

            if (character is '"' or '\'')
            {
                quote = character;
                current.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else if (character is '`' or '|' or ';' or '&' or '>' or '#')
            {
                // Shell operators, redirections, comments, and the end of a Markdown code span end the command line;
                // a file descriptor number before a redirection is not an argument.
                if (current.ToString().All(char.IsAsciiDigit))
                {
                    current.Clear();
                }

                break;
            }
            else
            {
                current.Append(character);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    private static string Unquote(string token) =>
        token.Length >= 2 && token[0] is '"' or '\'' && token[^1] == token[0] ? token[1..^1] : token;

    private static string Quote(string token) =>
        token.Any(char.IsWhiteSpace) ? $"\"{token}\"" : token;

    [GeneratedRegex(@"dotnet\s+(?:tool\s+run\s+(?<command>sharedkernel-[a-z0-9-]+)|run\s+--project\s+[^\n]*?tools/(?<project>SharedKernel\.[A-Za-z.]+?\.Tool)\b[^\n]*?\s--(?=\s|$))(?:\s+--(?=\s|$))?", RegexOptions.CultureInvariant)]
    private static partial Regex ToolCommandRegex();

    [GeneratedRegex(@"\$\{?(?:env:)?(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.CultureInvariant)]
    private static partial Regex VariableNameRegex();

    [GeneratedRegex(@"\$\{\{.*?\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex WorkflowExpressionRegex();
}
