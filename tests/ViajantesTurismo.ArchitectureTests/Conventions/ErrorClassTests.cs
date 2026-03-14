using ViajantesTurismo.ArchitectureTests.Infrastructure;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class ErrorClassTests
{
    [Fact]
    public void ErrorClasses_Must_Be_Static()
    {
        var errorClasses = GetErrorClasses();

        var violatingTypes = errorClasses
            .Where(type => !type.IsAbstract || !type.IsSealed)
            .ToArray();

        Assert.False(
            violatingTypes.Length != 0,
            $"Expected error classes to be static, but found violations: {string.Join(", ", violatingTypes.Select(t => t.FullName))}");
    }

    private static Type[] GetErrorClasses()
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
