using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.ArchitectureTests.Infrastructure;

namespace ViajantesTurismo.ArchitectureTests;

public sealed class ErrorClassTests
{
    private static readonly Type[] AggregateRootTypes =
    [
        typeof(Tour),
        typeof(Customer)
    ];

    [Fact]
    public void AggregateRoots_Must_Have_Corresponding_Errors_Class()
    {
        const string domainNamespace = ArchitectureProvider.Namespaces.Domain;
        var domainTypes = ArchitectureProvider.Assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.Namespace?.StartsWith(domainNamespace, StringComparison.Ordinal) == true)
            .ToArray();

        var violatingTypes = AggregateRootTypes
            .Where(aggregateRoot =>
            {
                var expectedErrorClassName = $"{aggregateRoot.Name}Errors";
                return domainTypes.All(t => t.Name != expectedErrorClassName);
            })
            .ToArray();

        Assert.False(
            violatingTypes.Length != 0,
            $"Expected aggregate roots to have a corresponding '*Errors' class, but found violations: {string.Join(", ", violatingTypes.Select(t => $"{t.Name} (missing {t.Name}Errors)"))}");
    }

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
