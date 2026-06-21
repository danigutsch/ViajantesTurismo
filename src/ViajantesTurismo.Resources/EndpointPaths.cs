namespace ViajantesTurismo.Resources;

/// <summary>
/// Contains shared HTTP endpoint paths used by hosted services and AppHost health checks.
/// </summary>
public static class EndpointPaths
{
    /// <summary>
    /// The health endpoint path used for readiness checks.
    /// </summary>
    public const string Health = "/health";

    /// <summary>
    /// The aliveness endpoint path used for liveness checks.
    /// </summary>
    public const string Aliveness = "/alive";
}
