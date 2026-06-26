using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static class DddConventionsTestsHelpers
{
    private static readonly Type EntityBaseType = typeof(Entity<>);
    private static readonly Type ValueObjectBaseType = typeof(ValueObject);

    public static bool InheritsFromEntity(Type type)
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
