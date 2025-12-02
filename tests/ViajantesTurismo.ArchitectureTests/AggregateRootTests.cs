using System.Collections;
using System.Reflection;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.ArchitectureTests;

public sealed class AggregateRootTests
{
    private static readonly Type[] AggregateRootTypes =
    [
        typeof(Tour),
        typeof(Customer)
    ];

    private static readonly Type[] AllowedCollectionTypes =
    [
        typeof(IReadOnlyList<>),
        typeof(IReadOnlyCollection<>)
    ];

    [Fact]
    public void AggregateRoots_Collection_Properties_Must_Return_ReadOnly_Types()
    {
        var violations = new List<string>();

        foreach (var aggregateRoot in AggregateRootTypes)
        {
            var properties = aggregateRoot.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (!IsCollectionType(property.PropertyType))
                {
                    continue;
                }

                if (!IsAllowedReadOnlyCollection(property.PropertyType))
                {
                    violations.Add($"{aggregateRoot.Name}.{property.Name} returns {property.PropertyType.Name}");
                }
            }
        }

        Assert.False(
            violations.Count != 0,
            $"Expected collection properties to return IReadOnlyList<T> or IReadOnlyCollection<T>, but found violations: {string.Join(", ", violations)}");
    }

    private static bool IsCollectionType(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        return type.IsGenericType &&
               (typeof(IEnumerable).IsAssignableFrom(type) ||
                type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    private static bool IsAllowedReadOnlyCollection(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericTypeDef = type.GetGenericTypeDefinition();
        return AllowedCollectionTypes.Any(allowed => allowed == genericTypeDef);
    }
}
