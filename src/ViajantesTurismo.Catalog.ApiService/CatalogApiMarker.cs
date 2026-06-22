using System.Reflection;

namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>
/// Provides access to the Catalog API assembly for architecture tests and composition roots.
/// </summary>
public static class CatalogApiMarker
{
    /// <summary>
    /// Gets the Catalog API assembly.
    /// </summary>
    public static Assembly Assembly => typeof(CatalogApiMarker).Assembly;
}
