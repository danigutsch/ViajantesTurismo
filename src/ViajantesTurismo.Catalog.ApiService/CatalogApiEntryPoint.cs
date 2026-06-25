using System.Reflection;

namespace ViajantesTurismo.Catalog.ApiService;

internal sealed class CatalogApiEntryPoint
{
    public Assembly Assembly { get; } = CatalogApiMarker.Assembly;
}
