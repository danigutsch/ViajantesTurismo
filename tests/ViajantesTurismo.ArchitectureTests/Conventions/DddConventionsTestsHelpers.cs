using SharedKernel.Domain;
using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static class DddConventionsTestsHelpers
{
    private static readonly Type EntityInterfaceType = typeof(IEntity<>);
    private static readonly Type ValueObjectBaseType = typeof(ValueObject);

    public static bool InheritsFromEntity(Type type)
    {
        return type.GetInterfaces().Any(static interfaceType =>
            interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == EntityInterfaceType);
    }

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
}
