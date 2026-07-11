namespace ViajantesTurismo.Performance.Tool;

internal static class PerformanceToolApplication
{
    private const string UsageMessage = "Usage: viajantes-performance <admin-smoke|file-upload-scan> [-- <k6-args>]";

    private const string ErrorPrefix = "performance-tool: ";

    public static async Task<int> Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            await output.WriteLineAsync(UsageMessage).ConfigureAwait(false);
            return args.Length == 0 ? 2 : 0;
        }

        try
        {
            if (string.Equals(args[0], "file-upload-scan", StringComparison.Ordinal))
            {
                var k6Arguments = args.Skip(1).SkipWhile(static value => value == "--").ToArray();
                return await FileUploadScanCommand.Run(k6Arguments, output).ConfigureAwait(false);
            }

            if (string.Equals(args[0], "admin-smoke", StringComparison.Ordinal))
            {
                var k6Arguments = args.Skip(1).SkipWhile(static value => value == "--").ToArray();
                return await AdminSmokeCommand.Run(k6Arguments).ConfigureAwait(false);
            }

            await error.WriteLineAsync(UsageMessage).ConfigureAwait(false);
            return 2;
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or ArgumentException
            or IOException
            or NotSupportedException
            or UnauthorizedAccessException)
        {
            await error.WriteLineAsync(ErrorPrefix + exception.Message).ConfigureAwait(false);
            return 1;
        }
    }

    private static bool IsHelp(string value)
    {
        return string.Equals(value, "-h", StringComparison.Ordinal)
            || string.Equals(value, "--help", StringComparison.Ordinal)
            || string.Equals(value, "help", StringComparison.Ordinal);
    }
}
