namespace ViajantesTurismo.Management.Web.Components.Pages.Operations;

/// <summary>Deployment-configured count limits for the read-only Operations dashboard.</summary>
internal sealed class OperationsDashboardThresholds
{
    /// <summary>Gets the maximum count by dashboard metric identifier.</summary>
    public Dictionary<string, long> MaximumCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers and validates the dashboard thresholds from the deployment configuration.</summary>
    internal static void AddOptions(IServiceCollection services) => services
        .AddOptions<OperationsDashboardThresholds>()
        .BindConfiguration("Operations:Dashboard")
        .Validate(options => options.MaximumCounts.Values.All(static count => count >= 0),
            "Operations dashboard count thresholds must be non-negative.")
        .ValidateOnStart();
}
