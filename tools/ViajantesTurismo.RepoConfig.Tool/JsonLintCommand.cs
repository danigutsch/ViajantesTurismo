using System.Text;
using System.Text.Json;

namespace ViajantesTurismo.RepoConfig.Tool;

internal static class JsonLintCommand
{
    public static async Task<int> Run(string[] args, TextWriter error, string root, CancellationToken ct)
    {
        if (args.Length != 0)
        {
            throw new ArgumentException("lint-json validates all tracked JSON files and does not accept arguments.");
        }

        var indexedPaths = await CandidateCompatibilityCommand.RunProcess(
            "git", ["ls-files", "-z", "--", "*.json"], root, TextWriter.Null, ct, trimOutput: false).ConfigureAwait(false);
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var failed = false;
        foreach (var path in indexedPaths.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(Path.Combine(root, path), ct).ConfigureAwait(false);
                var content = encoding.GetString(bytes);
                if (content.Length > 0 && content[0] == '\uFEFF')
                {
                    content = content[1..];
                }

                using var document = JsonDocument.Parse(content, options);
            }
            catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or DecoderFallbackException)
            {
                failed = true;
                await error.WriteLineAsync(RepoConfigToolApplication.EscapeControlCharacters($"{path}: {exception.Message}")).ConfigureAwait(false);
            }
        }

        return failed ? 1 : 0;
    }
}
