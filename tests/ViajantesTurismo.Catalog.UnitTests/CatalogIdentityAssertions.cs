using System.Reflection;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Assertions for generated-identity equality semantics of entities.</summary>
internal static class CatalogIdentityAssertions
{
    /// <summary>Sets the entity Id through its non-public setter using reflection.</summary>
    public static void SetId<T>(T entity, Guid id)
    {
        var idProperty = typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var nonNullIdProperty = idProperty.ShouldNotBeNull();
        var idSetter = nonNullIdProperty.GetSetMethod(nonPublic: true);
        var nonNullIdSetter = idSetter.ShouldNotBeNull();

        nonNullIdSetter.Invoke(entity, [id]);
    }

    /// <summary>Asserts equality and hash code follow the generated Id, and that an empty Id equals nothing.</summary>
    public static void AssertGeneratedIdentitySemantics<T>(T first, T second, T different)
        where T : class
    {
        var id = Guid.CreateVersion7();
        SetId(first, id);
        SetId(second, id);
        SetId(different, Guid.CreateVersion7());
        var set = new HashSet<T> { first };

        EqualityComparer<T>.Default.Equals(first, first).ShouldBeTrue();
        EqualityComparer<T>.Default.Equals(first, second).ShouldBeTrue();
        EqualityComparer<T>.Default.Equals(first, different).ShouldBeFalse();
        EqualityComparer<T>.Default.Equals(first, null).ShouldBeFalse();
        var firstHashCode = first is null ? 0 : first.GetHashCode();
        var secondHashCode = second is null ? 0 : second.GetHashCode();
        secondHashCode.ShouldBe(firstHashCode);
        set.ShouldContain(second);
        set.ShouldNotContain(different);

        SetId(second, Guid.Empty);

        EqualityComparer<T>.Default.Equals(first, second).ShouldBeFalse();
        EqualityComparer<T>.Default.Equals(second, first).ShouldBeFalse();
        set.ShouldNotContain(second);
    }
}
