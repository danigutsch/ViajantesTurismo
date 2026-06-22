using System.Reflection;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Provides access to the Catalog infrastructure assembly for architecture tests and composition roots.
/// </summary>
public static class CatalogInfrastructureMarker
{
    /// <summary>
    /// Gets the Catalog infrastructure assembly.
    /// </summary>
    public static Assembly Assembly => typeof(CatalogInfrastructureMarker).Assembly;
}
