using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ViajantesTurismo.Management.Web.Services;

/// <summary>Provides local, non-blocking primary-language guidance for accessibility editors.</summary>
internal sealed class MediaLanguageGuidance(IConfiguration configuration)
{
    private const int MinimumLetters = 40;
    private const double MinimumConfidence = 0.90;
    private const double MinimumMargin = 0.50;

    /// <summary>Checks the application's explicit allowlist of well-formed BCP 47 declarations.</summary>
    public static bool IsAllowedLanguageTag(string? tag) => string.Equals(tag, "en-US", StringComparison.OrdinalIgnoreCase)
        || string.Equals(tag, "pt-BR", StringComparison.OrdinalIgnoreCase);

    /// <summary>Interprets local engine output without treating detection as declaration validation.</summary>
    internal static string InterpretPrediction(string output, string declaredTag)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(declaredTag);
        var parts = output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (!IsAllowedLanguageTag(declaredTag) || parts.Length != 4
            || !parts[0].StartsWith("__label__", StringComparison.Ordinal)
            || !parts[2].StartsWith("__label__", StringComparison.Ordinal)
            || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var first)
            || !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var second)
            || !double.IsFinite(first) || !double.IsFinite(second)
            || first < MinimumConfidence || first > 1 || second < 0 || second > 1 || first - second < MinimumMargin)
        {
            return "indeterminate";
        }

        var primary = parts[0][9..];
        return primary is not ("en" or "pt") ? "indeterminate"
            : declaredTag.StartsWith(primary + "-", StringComparison.OrdinalIgnoreCase) ? "match" : "mismatch";
    }

    internal static string InterpretOutput(string output, string declaredTag, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(declaredTag);
        var predictions = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => InterpretPrediction(line, declaredTag)).ToArray();
        return expectedCount > 0 && predictions.Length == expectedCount && predictions.All(value => value == predictions[0]) ? predictions[0] : "indeterminate";
    }

    /// <summary>Runs the configured fastText engine locally; uncertain results never prevent approval.</summary>
    public async Task<string> Evaluate(string text, string declaredTag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(declaredTag);
        cancellationToken.ThrowIfCancellationRequested();
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var letters = text.Count(char.IsLetter);
        var nameLikeWords = words.Count(word => word.Length > 0 && char.IsUpper(word[0]));
        if (!IsAllowedLanguageTag(declaredTag) || letters < MinimumLetters || words.Length < 6
            || nameLikeWords * 2 > words.Length || text.Length > 2000)
        {
            return "indeterminate";
        }

        var executable = configuration["MediaLanguageIdentification:ExecutablePath"];
        var model = configuration["MediaLanguageIdentification:ModelPath"];
        if (string.IsNullOrWhiteSpace(executable) && string.IsNullOrWhiteSpace(model))
        {
            return "indeterminate";
        }

        if (string.IsNullOrWhiteSpace(executable) || string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("Language identification requires both executable and model paths.");
        }

        if (!Path.IsPathFullyQualified(executable) || !Path.IsPathFullyQualified(model))
        {
            throw new InvalidOperationException("Language identification executable and model paths must be absolute.");
        }

        await using (var modelStream = File.OpenRead(model))
        {
            if (modelStream.Length != 938013)
            {
                throw new InvalidOperationException("The language identification model size does not match the approved artifact.");
            }

            var digest = await SHA256.HashDataAsync(modelStream, cancellationToken);
            if (!Convert.ToHexString(digest).Equals("8f3472cfe8738a7b6099e8e999c3cbfae0dcd15696aac7d7738a8039db603e83", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The language identification model does not match the approved lid.176.ftz checksum.");
            }
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true,
                RedirectStandardError = true, CreateNoWindow = true, StandardInputEncoding = new UTF8Encoding(false)
            }
        };
        process.StartInfo.ArgumentList.Add("predict-prob");
        process.StartInfo.ArgumentList.Add(model);
        process.StartInfo.ArgumentList.Add("-");
        process.StartInfo.ArgumentList.Add("2");
        var started = false;
        try
        {
            process.Start();
            started = true;
            var outputTask = ReadBounded(process.StandardOutput, timeout.Token);
            var errorTask = ReadBounded(process.StandardError, timeout.Token);
            await process.StandardInput.WriteLineAsync(text.Replace('\r', ' ').Replace('\n', ' ').AsMemory(), timeout.Token);
            if (words.Length >= 12)
            {
                var midpoint = words.Length / 2;
                await process.StandardInput.WriteLineAsync(string.Join(' ', words[..midpoint]).AsMemory(), timeout.Token);
                await process.StandardInput.WriteLineAsync(string.Join(' ', words[midpoint..]).AsMemory(), timeout.Token);
            }
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            var output = await outputTask;
            await errorTask;
            if (process.ExitCode != 0)
            {
                return "indeterminate";
            }

            return InterpretOutput(output, declaredTag, words.Length >= 12 ? 3 : 1);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "indeterminate";
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode is 2 or 3)
        {
            return "indeterminate";
        }
        catch (IOException)
        {
            return "indeterminate";
        }
        finally
        {
            if (started && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
    }

    private static async Task<string> ReadBounded(StreamReader reader, CancellationToken cancellationToken)
    {
        var buffer = new char[1025];
        var count = 0;
        while (count < buffer.Length)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(count), cancellationToken);
            if (read == 0)
            {
                return new string(buffer, 0, count);
            }

            count += read;
        }

        throw new IOException("Language identification output exceeded its limit.");
    }
}
