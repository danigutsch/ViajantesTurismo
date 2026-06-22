using System.Reflection;
using SharedKernel.AspNet;
using ViajantesTurismo.Admin.ApiService;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

internal static class AdminEndpointDefinitionCatalog
{
    public static IReadOnlyList<EndpointDefinition> GetDefinitions()
    {
        return typeof(AdminEndpoints)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(nestedType => nestedType.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(property => property.PropertyType == typeof(EndpointDefinition))
            .Select(property => property.GetValue(null))
            .OfType<EndpointDefinition>()
            .ToArray();
    }
}
