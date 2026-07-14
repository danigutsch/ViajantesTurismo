namespace ViajantesTurismo.Resources;

/// <summary>
/// Identifies a product-specific Aspire resource composition.
/// </summary>
public enum HostedProfile
{
    /// <summary>
    /// Includes the complete local application composition.
    /// </summary>
    Full,

    /// <summary>
    /// Includes the complete application composition without local-only developer tooling.
    /// </summary>
    System,

    /// <summary>
    /// Includes only the dependencies required by the Admin API integration tests.
    /// </summary>
    Admin
}
