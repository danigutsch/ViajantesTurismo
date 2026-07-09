using System.Text.Json;

namespace SharedKernel.Documentation.Tool;

internal static class DocumentationToolApplication
{
    private const string Usage = "Usage: sharedkernel-docs generate --config <path> [--check]";

    public static async Task<int> Run(string[] args, TextWriter output, TextWriter error, string rootPath)
    {
        if (args is not ["generate", .. var commandArgs])
        {
            await error.WriteLineAsync(args.Length == 0 ? "Missing command." : $"Unknown command: {args[0]}").ConfigureAwait(false);
            await error.WriteLineAsync(Usage).ConfigureAwait(false);
            return 1;
        }

        return await RunGenerate(commandArgs, output, error, rootPath).ConfigureAwait(false);
    }

    private static async Task<int> RunGenerate(string[] args, TextWriter output, TextWriter error, string rootPath)
    {
        var configPath = string.Empty;
        var checkOnly = false;

        var index = 0;
        while (index < args.Length)
        {
            var arg = args[index];
            if (arg == "--check")
            {
                checkOnly = true;
                index++;
                continue;
            }

            if (arg == "--config")
            {
                if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    await error.WriteLineAsync("Missing required value for --config.").ConfigureAwait(false);
                    await error.WriteLineAsync(Usage).ConfigureAwait(false);
                    return 1;
                }

                configPath = args[index + 1];
                index += 2;
                continue;
            }

            await error.WriteLineAsync($"Unknown argument: {arg}").ConfigureAwait(false);
            await error.WriteLineAsync(Usage).ConfigureAwait(false);
            return 1;
        }

        if (string.IsNullOrWhiteSpace(configPath))
        {
            await error.WriteLineAsync("Missing required --config <path>.").ConfigureAwait(false);
            await error.WriteLineAsync(Usage).ConfigureAwait(false);
            return 1;
        }

        DocumentationGenerationResult result;
        try
        {
            result = DocumentationGenerator.Run(rootPath, configPath, checkOnly);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or JsonException or UnauthorizedAccessException or InvalidOperationException)
        {
            await error.WriteLineAsync($"Documentation generation failed: {exception.Message}").ConfigureAwait(false);
            return 1;
        }

        if (checkOnly && result.ChangedFiles.Count > 0)
        {
            await error.WriteLineAsync("Generated documentation is stale:").ConfigureAwait(false);
            foreach (var path in result.ChangedFiles)
            {
                await error.WriteLineAsync($"- {path}").ConfigureAwait(false);
            }

            await error.WriteLineAsync($"Run: sharedkernel-docs generate --config {configPath}").ConfigureAwait(false);
            return 1;
        }

        if (result.ChangedFiles.Count == 0)
        {
            await output.WriteLineAsync("Generated documentation is current.").ConfigureAwait(false);
            return 0;
        }

        await output.WriteLineAsync("Updated generated documentation:").ConfigureAwait(false);
        foreach (var path in result.ChangedFiles)
        {
            await output.WriteLineAsync($"- {path}").ConfigureAwait(false);
        }

        return 0;
    }
}
