namespace ViajantesTurismo.SecurityChecks.Tool;

internal static class SecurityChecksToolApplication
{
    private const string UsageMessage = "Usage: viajantes-security-checks baseline <repository-root>";

    private const string CurrentBaselineMessage = "Security baseline is current.";

    private const string ErrorPrefix = "security-checks: ";

    public static int Run(string[] args)
    {
        if (args is not ["baseline", var repositoryRoot])
        {
            Console.Error.WriteLine(UsageMessage);
            return 2;
        }

        try
        {
            BaselineCheckValidator.Validate(Path.GetFullPath(repositoryRoot));
            Console.Out.WriteLine(CurrentBaselineMessage);
            return 0;
        }
        catch (Exception error) when (error is InvalidOperationException
            or ArgumentException
            or IOException
            or NotSupportedException
            or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(ErrorPrefix + error.Message);
            return 1;
        }
    }
}
