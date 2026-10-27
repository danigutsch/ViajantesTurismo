using ViajantesTurismo.ArchitectureTests.Infrastructure;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for tests over the domain error classes.</summary>
internal static class ErrorClassTestsHelpers
{
    /// <summary>Returns the exported domain types whose names end with "Errors".</summary>
    public static Type[] GetErrorClasses()
    {
        const string domainNamespace = ArchitectureProvider.Namespaces.Domain;
        return
        [
            .. ArchitectureProvider.Assemblies
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => t.Namespace?.StartsWith(domainNamespace, StringComparison.Ordinal) == true
                            && t.Name.EndsWith("Errors", StringComparison.Ordinal))
        ];
    }
}
