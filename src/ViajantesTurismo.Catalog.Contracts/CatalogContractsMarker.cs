using System.Reflection;

namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Provides access to the Catalog contracts assembly for architecture tests and composition roots.
/// </summary>
public static class CatalogContractsMarker
{
    /// <summary>
    /// Gets the Catalog contracts assembly.
    /// </summary>
    public static Assembly Assembly => typeof(CatalogContractsMarker).Assembly;
}
