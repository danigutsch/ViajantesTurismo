using System.Reflection;
using PandaBear.BuildingBlocks;
using PandaBear.Domain;
using PandaBear.Results;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Reflection helpers for the DDD convention tests.</summary>
internal static class DddConventionsTestsHelpers
{
    private static readonly Type EntityInterfaceType = typeof(IEntity<>);
    private static readonly Type AggregateRootInterfaceType = typeof(IAggregateRoot);
    private static readonly Type ValueObjectBaseType = typeof(ValueObject);
    private static readonly Type ResultType = typeof(Result);
    private static readonly Type GenericResultType = typeof(Result<>);
    private static readonly Type EnumerableInterfaceType = typeof(IEnumerable<>);

    /// <summary>Reports whether the type implements the generic entity interface.</summary>
    public static bool InheritsFromEntity(Type type)
    {
        return type.GetInterfaces().Any(static interfaceType =>
            interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == EntityInterfaceType);
    }

    /// <summary>Reports whether the type derives, directly or indirectly, from the value object base class.</summary>
    public static bool InheritsFromValueObject(Type type)
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

    /// <summary>Returns the non-aggregate-root entity types that another entity in the set exposes as a property or collection.</summary>
    public static IReadOnlyCollection<Type> GetChildEntityTypes(IReadOnlyCollection<Type> entityTypes)
    {
        return
        [
            .. entityTypes.Where(candidate => !AggregateRootInterfaceType.IsAssignableFrom(candidate)
                && entityTypes.Any(owner => owner != candidate && ExposesEntity(owner, candidate)))
        ];
    }

    /// <summary>Lists the public constructors and declared public methods that return a result, as "Type.Member" names.</summary>
    public static IEnumerable<string> GetPublicMutators(Type type)
    {
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .Select(_ => $"{type.FullName}..ctor");
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(static method => !method.IsSpecialName && ReturnsResult(method))
            .Select(method => $"{type.FullName}.{method.Name}");

        return constructors.Concat(methods);
    }

    private static bool ExposesEntity(Type owner, Type entity)
    {
        return owner.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Any(property => property.PropertyType == entity || GetEnumerableElementType(property.PropertyType) == entity);
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == EnumerableInterfaceType)
        {
            return type.GetGenericArguments()[0];
        }

        return type.GetInterfaces()
            .FirstOrDefault(static interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == EnumerableInterfaceType)
            ?.GetGenericArguments()[0];
    }

    private static bool ReturnsResult(MethodInfo method)
    {
        return method.ReturnType == ResultType
            || (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == GenericResultType);
    }
}
