using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.SecurityChecks.Tool;

internal static class SecurityChecksToolApplication
{
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "CLI output is intentionally invariant.")]
    public static int Run(string[] args)
    {
        if (args is not ["baseline", var repositoryRoot])
        {
            Console.Error.WriteLine("Usage: viajantes-security-checks baseline <repository-root>");
            return 2;
        }

        try
        {
            BaselineCheckValidator.Validate(Path.GetFullPath(repositoryRoot));
            Console.WriteLine("Security baseline is current.");
            return 0;
        }
        catch (InvalidOperationException error)
        {
            Console.Error.WriteLine($"security-checks: {error.Message}");
            return 1;
        }
    }
}
