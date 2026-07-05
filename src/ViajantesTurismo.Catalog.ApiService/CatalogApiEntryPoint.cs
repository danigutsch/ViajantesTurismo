using System.Reflection;

namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>
/// Provides an assembly marker used by ASP.NET Core test hosts.
/// </summary>
internal sealed class CatalogApiEntryPoint
{
    /// <summary>
    /// Gets the API service assembly.
    /// </summary>
    public Assembly Assembly { get; } = CatalogApiMarker.Assembly;
}
