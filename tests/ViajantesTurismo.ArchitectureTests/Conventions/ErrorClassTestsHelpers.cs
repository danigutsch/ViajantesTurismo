using ViajantesTurismo.ArchitectureTests.Infrastructure;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static class ErrorClassTestsHelpers
{
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
