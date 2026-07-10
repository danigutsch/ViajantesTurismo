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
        catch (InvalidOperationException error)
        {
            Console.Error.WriteLine(ErrorPrefix + error.Message);
            return 1;
        }
        catch (ArgumentException error)
        {
            Console.Error.WriteLine(ErrorPrefix + error.Message);
            return 1;
        }
        catch (IOException error)
        {
            Console.Error.WriteLine(ErrorPrefix + error.Message);
            return 1;
        }
        catch (NotSupportedException error)
        {
            Console.Error.WriteLine(ErrorPrefix + error.Message);
            return 1;
        }
        catch (UnauthorizedAccessException error)
        {
            Console.Error.WriteLine(ErrorPrefix + error.Message);
            return 1;
        }
    }
}
