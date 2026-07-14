namespace ViajantesTurismo.Resources;

/// <summary>
/// Converts product hosting profiles to and from AppHost command-line arguments.
/// </summary>
public static class HostedProfileArguments
{
    private const string AdminProfileArgument = "--hosted-profile=admin";
    private const string SystemProfileArgument = "--hosted-profile=system";
    private const string ProfileArgumentPrefix = "--hosted-profile=";

    /// <summary>
    /// Gets the AppHost arguments for a product hosting profile.
    /// </summary>
    /// <param name="profile">The selected hosting profile.</param>
    /// <returns>The arguments that select the requested profile.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="profile"/> is unsupported.</exception>
    public static string[] ToArguments(this HostedProfile profile)
    {
        return profile switch
        {
            HostedProfile.Admin => [AdminProfileArgument],
            HostedProfile.System => [SystemProfileArgument],
            HostedProfile.Full => [],
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unsupported hosted profile.")
        };
    }

    /// <summary>
    /// Gets the selected hosting profile, defaulting to the complete composition.
    /// </summary>
    /// <param name="args">The AppHost command-line arguments.</param>
    /// <returns>The selected hosting profile.</returns>
    public static HostedProfile FromArguments(IEnumerable<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var profileArguments = args
            .Where(argument => argument.StartsWith(ProfileArgumentPrefix, StringComparison.Ordinal))
            .ToArray();

        return profileArguments.Length switch
        {
            0 => HostedProfile.Full,
            1 when string.Equals(profileArguments[0], AdminProfileArgument, StringComparison.Ordinal) => HostedProfile.Admin,
            1 when string.Equals(profileArguments[0], SystemProfileArgument, StringComparison.Ordinal) => HostedProfile.System,
            1 => throw new ArgumentOutOfRangeException(nameof(args), profileArguments[0], "Unsupported hosted profile."),
            _ => throw new ArgumentException("Only one hosted profile may be selected.", nameof(args))
        };
    }
}
