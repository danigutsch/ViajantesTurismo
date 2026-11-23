namespace ViajantesTurismo.Resources;

/// <summary>Names the planned HTTP maintenance switch shared by the application hosts.</summary>
public static class HttpMaintenanceConfiguration
{
    /// <summary>The configuration key for planned HTTP maintenance.</summary>
    public const string EnabledKey = "Maintenance:Enabled";

    /// <summary>The environment variable that configures the same switch.</summary>
    public const string EnabledEnvironmentVariable = "Maintenance__Enabled";

    /// <summary>Parses the configured startup switch, which defaults to disabled.</summary>
    /// <param name="value">The configured value, or <see langword="null"/> when absent.</param>
    /// <returns>Whether the HTTP host should apply scheduled maintenance.</returns>
    public static bool IsEnabled(string? value)
    {
        if (value is null)
        {
            return false;
        }

        return bool.TryParse(value, out var enabled)
            ? enabled
            : throw new InvalidOperationException($"Configuration setting '{EnabledKey}' must be true or false.");
    }
}
