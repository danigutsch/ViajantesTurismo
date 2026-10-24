using System.Buffers;
using System.Globalization;
using System.Text;

namespace ViajantesTurismo.RepoConfig.Tool;

internal static class RepoConfigToolApplication
{
    private const string CommandHelp = "Commands: verify-sharedkernel-candidate, select-ci-test-projects, restore-ci-projects, run-ci-test-slice, verify-coverage, write-build-evidence, verify-build-evidence, plan-dependency-review.";

    public static async Task<int> Run(string[] args, TextWriter output, TextWriter error, string workingDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        if (args is [] or ["--help"] or ["-h"])
        {
            await output.WriteLineAsync(CommandHelp).ConfigureAwait(false);
            return 0;
        }

        try
        {
            return args.FirstOrDefault() switch
            {
                "restore-ci-projects" => await CiRestoreCommand.Run(args[1..], output, workingDirectory, cancellationToken).ConfigureAwait(false),
                "run-ci-test-slice" => await CiTestSliceCommand.Run(args[1..], output, workingDirectory, cancellationToken).ConfigureAwait(false),
                "write-build-evidence" => await BuildEvidenceCommand.Run(true, args[1..], workingDirectory, cancellationToken).ConfigureAwait(false),
                "verify-build-evidence" => await BuildEvidenceCommand.Run(false, args[1..], workingDirectory, cancellationToken).ConfigureAwait(false),
                "plan-dependency-review" => await DependencyReviewCommand.Run(args[1..], output, workingDirectory, cancellationToken).ConfigureAwait(false),
                "verify-sharedkernel-candidate" => await CandidateCompatibilityCommand.Run(args[1..], output, error, workingDirectory, cancellationToken).ConfigureAwait(false),
                "select-ci-test-projects" => await CiTestProjectSelectionCommand.Run(args[1..], output, error, workingDirectory, cancellationToken).ConfigureAwait(false),
                "verify-coverage" => await CoverageGateCommand.Run(args[1..], output, error, workingDirectory, cancellationToken).ConfigureAwait(false),
                _ => throw new ArgumentException(CommandHelp + " Use sharedkernel-repo for reusable repository commands."),
            };
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException or System.Text.Json.JsonException or TimeoutException or KeyNotFoundException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            await error.WriteLineAsync(EscapeControlCharacters(exception.Message)).ConfigureAwait(false);
            return 1;
        }
    }
    internal static string EscapeControlCharacters(string value)
    {
        StringBuilder? escaped = null;
        var index = 0;
        while (index < value.Length)
        {
            var status = Rune.DecodeFromUtf16(value.AsSpan(index), out var rune, out var consumed);
            if (status != OperationStatus.Done)
            {
                escaped ??= new StringBuilder(value.Length + 6).Append(value, 0, index);
                escaped.Append("\\u").Append(((int)value[index]).ToString("X4", CultureInfo.InvariantCulture));
                index++;
                continue;
            }

            var category = Rune.GetUnicodeCategory(rune);
            if (category is not UnicodeCategory.Control
                    and not UnicodeCategory.Format
                    and not UnicodeCategory.LineSeparator
                    and not UnicodeCategory.ParagraphSeparator)
            {
                escaped?.Append(value, index, consumed);
                index += consumed;
                continue;
            }

            escaped ??= new StringBuilder(value.Length + 6).Append(value, 0, index);
            escaped
                .Append(rune.IsBmp ? "\\u" : "\\U")
                .Append(rune.Value.ToString(rune.IsBmp ? "X4" : "X8", CultureInfo.InvariantCulture));
            index += consumed;
        }

        return escaped?.ToString() ?? value;
    }
}
