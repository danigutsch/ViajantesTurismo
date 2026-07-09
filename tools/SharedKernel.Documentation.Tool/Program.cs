namespace SharedKernel.Documentation.Tool;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args is not ["generate", .. var commandArgs])
        {
            await Console.Error.WriteLineAsync(args.Length == 0 ? "Missing command." : $"Unknown command: {args[0]}").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(Usage).ConfigureAwait(false);
            return 1;
        }

        return await RunGenerate(commandArgs).ConfigureAwait(false);
    }

    private static async Task<int> RunGenerate(string[] args)
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

            if (arg == "--config" && index + 1 < args.Length)
            {
                configPath = args[index + 1];
                index += 2;
                continue;
            }

            await Console.Error.WriteLineAsync($"Unknown argument: {arg}").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(Usage).ConfigureAwait(false);
            return 1;
        }

        if (string.IsNullOrWhiteSpace(configPath))
        {
            await Console.Error.WriteLineAsync("Missing required --config <path>.").ConfigureAwait(false);
            return 1;
        }

        var result = DocumentationGenerator.Run(Environment.CurrentDirectory, configPath, checkOnly);
        if (checkOnly && result.ChangedFiles.Count > 0)
        {
            await Console.Error.WriteLineAsync("Generated documentation is stale:").ConfigureAwait(false);
            foreach (var path in result.ChangedFiles)
            {
                await Console.Error.WriteLineAsync($"- {path}").ConfigureAwait(false);
            }

            await Console.Error.WriteLineAsync($"Run: sharedkernel-docs generate --config {configPath}").ConfigureAwait(false);
            return 1;
        }

        if (result.ChangedFiles.Count == 0)
        {
            await Console.Out.WriteLineAsync("Generated documentation is current.").ConfigureAwait(false);
            return 0;
        }

        await Console.Out.WriteLineAsync("Updated generated documentation:").ConfigureAwait(false);
        foreach (var path in result.ChangedFiles)
        {
            await Console.Out.WriteLineAsync($"- {path}").ConfigureAwait(false);
        }

        return 0;
    }

    private const string Usage = "Usage: sharedkernel-docs generate --config <path> [--check]";
}
