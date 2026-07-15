using System.Diagnostics;

namespace ViajantesTurismo.OpenApi.Tool;

internal static class OpenApiToolApplication
{
    private const string Usage = "Usage: viajantes-openapi generate <admin|catalog|branding> [--refresh]";

    public static async Task<int> Run(string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args is [] or ["--help"] or ["-h"])
        {
            await output.WriteLineAsync(Usage).ConfigureAwait(false);
            return 0;
        }

        try
        {
            var options = OpenApiGenerationOptions.Parse(args, FindRepositoryRoot(Directory.GetCurrentDirectory()));
            var startInfo = OpenApiGenerationCommand.CreateStartInfo(
                options,
                string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.Ordinal));
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start dotnet build.");
            await process.WaitForExitAsync().ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (ArgumentException exception)
        {
            await error.WriteLineAsync($"Error: {exception.Message}").ConfigureAwait(false);
            return 2;
        }
    }

    internal static string FindRepositoryRoot(string startDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);

        var currentDirectory = new DirectoryInfo(startDirectory);

        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "ViajantesTurismo.slnx")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate ViajantesTurismo.slnx from '{startDirectory}'.");
    }
}
