using System.Reflection;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Assertions and reflection helpers for entity identifier tests.</summary>
internal static class EntityIdAssertions
{
    /// <summary>Asserts the identifier is a version 7 GUID.</summary>
    public static void AssertUuidV7(Guid id)
    {
        var guidText = id.ToString("D");
        guidText[14].ShouldBe('7');
    }

    /// <summary>Sets the entity Id through its non-public setter via reflection.</summary>
    public static void SetId<T>(T entity, Guid id)
    {
        var idProperty = typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var nonNullIdProperty = idProperty.ShouldNotBeNull();
        var idSetter = nonNullIdProperty.GetSetMethod(nonPublic: true);
        var nonNullIdSetter = idSetter.ShouldNotBeNull();

        nonNullIdSetter.Invoke(entity, [id]);
    }

    /// <summary>Asserts that equality and hashing of generated-identity entities follow their Id: equal for same Id, different otherwise, and never equal once an Id is empty.</summary>
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
