using System.Reflection;

namespace ViajantesTurismo.Catalog.Application;

/// <summary>
/// Provides access to the Catalog application assembly for architecture tests and composition roots.
/// </summary>
public static class CatalogApplicationMarker
{
    /// <summary>
    /// Gets the Catalog application assembly.
    /// </summary>
    public static Assembly Assembly => typeof(CatalogApplicationMarker).Assembly;
}
