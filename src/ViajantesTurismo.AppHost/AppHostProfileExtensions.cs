using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

internal static class AppHostProfileExtensions
{
    private const string DatabaseObservabilityFeatureConfigurationKey = "Aspire:Features:DatabaseObservability";

    public static bool IncludesDeveloperTooling(this HostedProfile profile)
    {
        return profile is HostedProfile.Full;
    }

    public static bool IncludesMediaInfrastructure(this HostedProfile profile)
    {
        return profile is HostedProfile.Full or HostedProfile.System;
    }

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
