using System.Reflection;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.ArchitectureTests.Infrastructure;
using static ViajantesTurismo.ArchitectureTests.Conventions.DddConventionsTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class DddConventionsTests
{
    private static IEnumerable<Type> EntityTypes =>
    [
        .. ArchitectureProvider.Assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && InheritsFromEntity(type))
    ];

    private static IEnumerable<Type> ValueObjectTypes =>
    [
        .. ArchitectureProvider.Assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && InheritsFromValueObject(type))
    ];

    [Fact]
    public void Entities_must_be_sealed()
    {
        var violatingTypes = EntityTypes.Where(type => !type.IsSealed).ToArray();
        var areDifferent = violatingTypes.Length != 0;
        areDifferent.ShouldBeFalse(
            $"Expected all entities to be sealed, but found: {string.Join(", ", violatingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void Entities_must_be_located_in_domain_namespace()
    {
        var domainNamespaces = new[]
        {
            ArchitectureProvider.Namespaces.Domain,
            ArchitectureProvider.Namespaces.CatalogDomain
        };
        var violatingTypes = EntityTypes
            .Where(type => type.Namespace is null || !domainNamespaces.Any(domainNamespace =>
                type.Namespace.StartsWith(domainNamespace, StringComparison.Ordinal)))
            .ToArray();

        var areDifferent1 = violatingTypes.Length != 0;
        areDifferent1.ShouldBeFalse(
            $"Expected all entities to reside in one of [{string.Join(", ", domainNamespaces)}], but found: {string.Join(", ", violatingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void ValueObjects_must_be_sealed()
    {
        var violatingTypes = ValueObjectTypes.Where(type => !type.IsSealed).ToArray();
        var areDifferent2 = violatingTypes.Length != 0;
        areDifferent2.ShouldBeFalse(
            $"Expected all value objects to be sealed, but found: {string.Join(", ", violatingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void ValueObjects_should_not_expose_public_setters()
    {
        var violatingTypes = ValueObjectTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => $"{property.DeclaringType!.FullName}.{property.Name}")
            .ToArray();

        var areDifferent3 = violatingTypes.Length != 0;
        areDifferent3.ShouldBeFalse(
            $"Expected value objects to expose immutable state, but found public setters on: {string.Join(", ", violatingTypes)}");
    }

    [Fact]
    public void Child_entities_must_change_only_through_their_aggregate_root()
    {
        // Arrange
        var childEntityTypes = GetChildEntityTypes([.. EntityTypes]);

        // Act
        var publicMutators = childEntityTypes.SelectMany(GetPublicMutators).ToArray();

        // Assert
        childEntityTypes.ShouldContain(typeof(Booking));
        publicMutators.ShouldBeEmpty();
    }

}
