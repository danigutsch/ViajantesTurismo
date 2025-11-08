using System.Reflection;
using ViajantesTurismo.ArchitectureTests.Infrastructure;
using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.ArchitectureTests;

public sealed class DddConventionsTests
{
    private static readonly Type EntityBaseType = typeof(Entity<>);
    private static readonly Type ValueObjectBaseType = typeof(ValueObject);

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
    public void Entities_Must_Be_Sealed()
    {
        var violatingTypes = EntityTypes.Where(type => !type.IsSealed).ToArray();
        Assert.False(
            violatingTypes.Length != 0,
            $"Expected all entities to be sealed, but found: {string.Join(", ", violatingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void Entities_Must_Be_Located_In_Domain_Namespace()
    {
        var domainNamespace = ArchitectureProvider.Namespaces.Domain;
        var violatingTypes = EntityTypes
            .Where(type => type.Namespace is null || !type.Namespace.StartsWith(domainNamespace, StringComparison.Ordinal))
            .ToArray();

        Assert.False(
            violatingTypes.Length != 0,
            $"Expected all entities to reside in namespace '{domainNamespace}', but found: {string.Join(", ", violatingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void ValueObjects_Must_Be_Sealed()
    {
        var violatingTypes = ValueObjectTypes.Where(type => !type.IsSealed).ToArray();
        Assert.False(
            violatingTypes.Length != 0,
            $"Expected all value objects to be sealed, but found: {string.Join(", ", violatingTypes.Select(t => t.FullName))}");
    }

    [Fact]
    public void ValueObjects_Should_Not_Expose_Public_Setters()
    {
        var violatingTypes = ValueObjectTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => $"{property.DeclaringType!.FullName}.{property.Name}")
            .ToArray();

        Assert.False(
            violatingTypes.Length != 0,
            $"Expected value objects to expose immutable state, but found public setters on: {string.Join(", ", violatingTypes)}");
    }

    private static bool InheritsFromEntity(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == EntityBaseType)
            {
                return true;
            }
        }

        return false;
    }

    private static bool InheritsFromValueObject(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current == ValueObjectBaseType)
            {
                return true;
            }
        }

        return false;
    }
}