using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>Hosted-profile feature switches for the AppHost.</summary>
internal static class AppHostProfileExtensions
{
    private const string DatabaseObservabilityFeatureConfigurationKey = "Aspire:Features:DatabaseObservability";

    /// <summary>Returns true when the profile starts developer tooling (full profile only).</summary>
    public static bool IncludesDeveloperTooling(this HostedProfile profile)
    {
        return profile is HostedProfile.Full;
    }

    /// <summary>Returns true when the profile includes media infrastructure (full and system profiles).</summary>
    public static bool IncludesMediaInfrastructure(this HostedProfile profile)
    {
        return profile is HostedProfile.Full or HostedProfile.System;
    }

    /// <summary>Returns true when database observability is enabled by profile and by the feature configuration key.</summary>
    public static bool EnablesDatabaseObservability(
        this IDistributedApplicationBuilder builder,
        HostedProfile profile)
    {
        return profile.IncludesDeveloperTooling()
            && string.Equals(
                builder.Configuration[DatabaseObservabilityFeatureConfigurationKey],
                bool.TrueString,
                StringComparison.OrdinalIgnoreCase);
    }
}
