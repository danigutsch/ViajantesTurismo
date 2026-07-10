using System.Reflection;

namespace ViajantesTurismo.Branding.ApiService;

/// <summary>
/// Provides access to the Branding API assembly for architecture tests and composition roots.
/// </summary>
public static class BrandingApiMarker
{
    /// <summary>
    /// Gets the Branding API assembly.
    /// </summary>
    public static Assembly Assembly => typeof(BrandingApiMarker).Assembly;
}
