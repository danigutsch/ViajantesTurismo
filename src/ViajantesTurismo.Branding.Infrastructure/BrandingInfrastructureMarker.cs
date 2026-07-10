using System.Reflection;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>
/// Provides access to the Branding infrastructure assembly for architecture tests and composition roots.
/// </summary>
public static class BrandingInfrastructureMarker
{
    /// <summary>
    /// Gets the Branding infrastructure assembly.
    /// </summary>
    public static Assembly Assembly => typeof(BrandingInfrastructureMarker).Assembly;
}
