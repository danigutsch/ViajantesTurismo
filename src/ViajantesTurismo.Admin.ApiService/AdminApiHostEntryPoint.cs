using System.Reflection;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Provides an assembly marker used by ASP.NET Core test hosts.
/// </summary>
internal sealed class AdminApiHostEntryPoint
{
    /// <summary>
    /// Gets the API service assembly.
    /// </summary>
    public Assembly Assembly { get; } = ApiMarker.Assembly;
}
