using System.Reflection;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

internal static class EntityIdAssertions
{
    public static void AssertUuidV7(Guid id)
    {
        var guidText = id.ToString("D");
        TestAssert.Equal('7', guidText[14]);
    }

    public static void SetId<T>(T entity, Guid id)
    {
        var idProperty = typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var nonNullIdProperty = TestAssert.NotNull(idProperty);
        var idSetter = nonNullIdProperty.GetSetMethod(nonPublic: true);
        var nonNullIdSetter = TestAssert.NotNull(idSetter);

        nonNullIdSetter.Invoke(entity, [id]);
    }

    public static void AssertGeneratedIdentitySemantics<T>(T first, T second, T different)
        where T : class
    {
        var id = Guid.CreateVersion7();
        SetId(first, id);
        SetId(second, id);
        SetId(different, Guid.CreateVersion7());
        var set = new HashSet<T> { first };

        TestAssert.True(EqualityComparer<T>.Default.Equals(first, first));
        TestAssert.True(EqualityComparer<T>.Default.Equals(first, second));
        TestAssert.False(EqualityComparer<T>.Default.Equals(first, different));
        TestAssert.False(EqualityComparer<T>.Default.Equals(first, null));
        var firstHashCode = first is null ? 0 : first.GetHashCode();
        var secondHashCode = second is null ? 0 : second.GetHashCode();
        TestAssert.Equal(firstHashCode, secondHashCode);
        TestAssert.Contains(second, set);
        TestAssert.DoesNotContain(different, set);

        SetId(second, Guid.Empty);

        TestAssert.False(EqualityComparer<T>.Default.Equals(first, second));
        TestAssert.False(EqualityComparer<T>.Default.Equals(second, first));
        TestAssert.DoesNotContain(second, set);
    }
}
