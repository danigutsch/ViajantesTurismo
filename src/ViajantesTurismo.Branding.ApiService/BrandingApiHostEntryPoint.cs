using System.Reflection;

namespace ViajantesTurismo.Branding.ApiService;

/// <summary>
/// Provides an assembly marker used by ASP.NET Core test hosts.
/// </summary>
internal sealed class BrandingApiHostEntryPoint
{
    /// <summary>
    /// Gets the API service assembly.
    /// </summary>
    public Assembly Assembly { get; } = BrandingApiMarker.Assembly;
}
