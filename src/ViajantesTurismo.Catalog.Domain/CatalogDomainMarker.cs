using System.Reflection;

namespace ViajantesTurismo.Catalog.Domain;

/// <summary>
/// Provides access to the Catalog domain assembly for architecture tests and composition roots.
/// </summary>
public static class CatalogDomainMarker
{
    /// <summary>
    /// Gets the Catalog domain assembly.
    /// </summary>
    public static Assembly Assembly => typeof(CatalogDomainMarker).Assembly;
}
