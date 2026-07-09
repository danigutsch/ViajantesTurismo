using System.Text;
using System.Text.RegularExpressions;

namespace SharedKernel.Documentation;

/// <summary>
/// Builds Markdown endpoint inventories from simple Minimal API route declarations.
/// </summary>
internal static partial class EndpointRouteMarkdownTable
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    /// <summary>
    /// Builds an endpoint inventory table from C# source files.
    /// </summary>
    public static string Build(string sourcePath, IReadOnlyDictionary<string, string> routePrefixes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(routePrefixes);

        var rows = SourceFiles(sourcePath)
            .SelectMany(file => RoutesFromFile(sourcePath, file, routePrefixes))
            .OrderBy(route => route.Path, StringComparer.Ordinal)
            .ThenBy(route => route.Method, StringComparer.Ordinal)
            .ThenBy(route => route.Name, StringComparer.Ordinal)
            .Select(route => $"| `{route.Method}` | `{route.Path}` | {Escape(route.Name)} | {route.Audience} | {route.Auth} | `{route.Source}` |")
            .ToArray();

        return string.Join(
            '\n',
            [
                "| Method | Route | Endpoint | Audience | Auth metadata | Source |",
                "| --- | --- | --- | --- | --- | --- |",
                .. (rows.Length == 0 ? ["| n/a | n/a | No Minimal API routes discovered. | n/a | n/a | n/a |"] : rows),
            ]);
    }

    private static IEnumerable<(string Method, string Path, string Name, string Audience, string Auth, string Source)> RoutesFromFile(
        string sourcePath,
        FileInfo file,
        IReadOnlyDictionary<string, string> configuredRoutePrefixes)
    {
        var lines = File.ReadAllLines(file.FullName, Utf8NoBom);
        var routePrefixes = new Dictionary<string, string>(configuredRoutePrefixes, StringComparer.Ordinal);
        AddInlineRouteGroups(lines, routePrefixes);

        for (var index = 0; index < lines.Length; index++)
        {
            var match = EndpointMapRegex().Match(lines[index]);
            if (!match.Success)
            {
                continue;
            }

            var receiver = match.Groups[1].Value;
            var method = match.Groups[2].Value.ToUpperInvariant();
            var path = FullPath(receiver, match.Groups[3].Value, routePrefixes);
            var chain = EndpointChain(lines, index);
            var handler = match.Groups[4].Value.Trim();
            var source = Path.GetRelativePath(sourcePath, file.FullName).Replace(Path.DirectorySeparatorChar, '/');
            yield return (method, path, EndpointName(chain, handler), Audience(path, source), AuthMetadata(chain), source);
        }
    }

    private static IEnumerable<FileInfo> SourceFiles(string sourcePath) =>
        new DirectoryInfo(sourcePath)
            .EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.FullName.Split(Path.DirectorySeparatorChar).Contains("bin", StringComparer.Ordinal))
            .Where(static file => !file.FullName.Split(Path.DirectorySeparatorChar).Contains("obj", StringComparer.Ordinal))
            .OrderBy(file => file.FullName, StringComparer.Ordinal);

    private static void AddInlineRouteGroups(string[] lines, Dictionary<string, string> routePrefixes)
    {
        foreach (var line in lines)
        {
            var match = RouteGroupRegex().Match(line);
            if (match.Success)
            {
                routePrefixes[match.Groups[1].Value] = match.Groups[2].Value;
            }
        }
    }

    private static string FullPath(string receiver, string route, Dictionary<string, string> routePrefixes)
    {
        var prefix = routePrefixes.TryGetValue(receiver, out var configuredPrefix) ? configuredPrefix : string.Empty;
        if (prefix.Length == 0)
        {
            return NormalizePath(route);
        }

        var suffix = route == "/" ? string.Empty : route.TrimStart('/');
        var combined = suffix.Length == 0 ? prefix : $"{prefix.TrimEnd('/')}/{suffix}";
        return TrimTrailingSlash(NormalizePath(combined));
    }

    private static string NormalizePath(string path) => path.StartsWith('/') ? path : $"/{path}";

    private static string TrimTrailingSlash(string path) => path.Length > 1 ? path.TrimEnd('/') : path;

    private static string EndpointChain(string[] lines, int startIndex)
    {
        var builder = new StringBuilder();
        for (var index = startIndex; index < lines.Length && index < startIndex + 8; index++)
        {
            builder.Append(lines[index]);
            if (lines[index].Contains(';', StringComparison.Ordinal))
            {
                break;
            }
        }

        return builder.ToString();
    }

    private static string EndpointName(string chain, string handler)
    {
        var metadata = AdminMetadataRegex().Match(chain);
        if (metadata.Success)
        {
            return metadata.Groups[1].Value;
        }

        var name = EndpointNameRegex().Match(chain);
        if (name.Success)
        {
            return name.Groups[1].Value;
        }

        return handler.Contains("=>", StringComparison.Ordinal) ? "inline" : handler;
    }

    private static string Audience(string path, string source)
    {
        if (source.StartsWith("ViajantesTurismo.Public.Web/", StringComparison.Ordinal))
        {
            return "public web";
        }

        return path.StartsWith("/public/", StringComparison.Ordinal) ? "public API" : "management/internal";
    }

    private static string AuthMetadata(string chain)
    {
        if (chain.Contains(".RequireAuthorization", StringComparison.Ordinal))
        {
            return "required";
        }

        return chain.Contains(".AllowAnonymous", StringComparison.Ordinal) ? "anonymous" : "not declared";
    }

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    [GeneratedRegex(@"^\s*var\s+(\w+)\s*=\s*app\.MapGroup\(""([^""]+)""\)", RegexOptions.CultureInvariant)]
    private static partial Regex RouteGroupRegex();

    [GeneratedRegex(@"^\s*(\w+)\.Map(Get|Post|Put|Delete|Patch)\(""([^""]+)""\s*,\s*([^;]+?)\)?\s*(?:;|$)", RegexOptions.CultureInvariant)]
    private static partial Regex EndpointMapRegex();

    [GeneratedRegex(@"\.WithName\(""([^""]+)""\)", RegexOptions.CultureInvariant)]
    private static partial Regex EndpointNameRegex();

    [GeneratedRegex(@"\.WithAdminMetadata\(""([^""]+)""", RegexOptions.CultureInvariant)]
    private static partial Regex AdminMetadataRegex();
}
